using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.App.Playback;

public sealed class MpvWorkerBackend : IPlayerBackend
{
    private readonly string workerPath;
    private readonly string mpvPath;
    private readonly Channel<PlayerSnapshot> snapshots = Channel.CreateUnbounded<PlayerSnapshot>();
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Dictionary<long, TaskCompletionSource<VideoFrame>> frameRequests = [];
    private readonly object frameLock = new();
    private NamedPipeClientStream? pipe;
    private StreamReader? reader;
    private StreamWriter? writer;
    private Process? process;
    private Task? readLoop;
    private long requestId;

    public MpvWorkerBackend(string? workerPath = null, string? mpvPath = null)
    {
        this.workerPath = workerPath ?? Path.Combine(AppContext.BaseDirectory, "PUPlayer.MpvWorker.exe");
        this.mpvPath = mpvPath ?? Path.Combine(AppContext.BaseDirectory, "libmpv-2.dll");
    }

    public IAsyncEnumerable<PlayerSnapshot> Snapshots(CancellationToken cancellationToken) =>
        snapshots.Reader.ReadAllAsync(cancellationToken);

    public async Task LoadAsync(string path, nint windowHandle, CancellationToken cancellationToken)
    {
        if (process is null) await StartAsync(windowHandle, cancellationToken);
        await SendAsync(PlayerRequest.Load.Create(NextId(), path), cancellationToken);
    }

    public Task SetPausedAsync(bool value, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.SetPause(NextId(), value), cancellationToken);

    public Task SeekAsync(double seconds, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.Seek(NextId(), seconds), cancellationToken);

    public Task SetVolumeAsync(double percent, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.SetVolume(NextId(), percent), cancellationToken);

    public Task SetSpeedAsync(double speed, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.SetSpeed(NextId(), speed), cancellationToken);

    public Task SetTransformAsync(MpvTransform transform, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.SetTransform(NextId(), transform), cancellationToken);

    public Task SetGeometryAsync(VideoTransform geometry, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.SetGeometry(NextId(), geometry), cancellationToken);

    public Task SetAudioFilterAsync(string value, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.SetAudioFilter(NextId(), value), cancellationToken);

    public Task LoadExternalAudioAsync(string path, CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.LoadExternalAudio(NextId(), path), cancellationToken);

    public Task UseOriginalAudioAsync(CancellationToken cancellationToken) =>
        SendAsync(new PlayerRequest.UseOriginalAudio(NextId()), cancellationToken);

    public async Task<VideoFrame> CaptureFrameAsync(CancellationToken cancellationToken)
    {
        var id = NextId();
        var completion = new TaskCompletionSource<VideoFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (frameLock) frameRequests[id] = completion;
        try
        {
            await SendAsync(new PlayerRequest.CaptureFrame(id), cancellationToken);
            return await completion.Task.WaitAsync(TimeSpan.FromMilliseconds(300), cancellationToken);
        }
        finally { lock (frameLock) frameRequests.Remove(id); }
    }

    public async ValueTask DisposeAsync()
    {
        if (writer is not null)
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            try { await SendAsync(new PlayerRequest.Shutdown(NextId()), shutdown.Token); } catch { }
        }
        pipe?.Dispose();
        if (readLoop is not null)
            try { await readLoop.WaitAsync(TimeSpan.FromMilliseconds(500)); } catch { }
        snapshots.Writer.TryComplete();
        if (process is { HasExited: false })
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(true); }
        }
        process?.Dispose();
        sendLock.Dispose();
    }

    private async Task StartAsync(nint windowHandle, CancellationToken cancellationToken)
    {
        if (!File.Exists(workerPath) || !File.Exists(mpvPath))
            throw new FileNotFoundException("No se encontraron PUPlayer.MpvWorker.exe y libmpv-2.dll junto a PUPlayer.");

        var pipeName = $"PUPlayer-{Guid.NewGuid():N}";
        var token = RandomNumberGenerator.GetHexString(64);
        var start = new ProcessStartInfo(workerPath) { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("--pipe"); start.ArgumentList.Add(pipeName);
        start.ArgumentList.Add("--token"); start.ArgumentList.Add(token);
        start.ArgumentList.Add("--wid"); start.ArgumentList.Add(unchecked((uint)windowHandle.ToInt64()).ToString(CultureInfo.InvariantCulture));
        start.ArgumentList.Add("--mpv"); start.ArgumentList.Add(Path.GetFullPath(mpvPath));
        process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar el motor de video.");

        pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        await pipe.ConnectAsync(10_000, cancellationToken);
        reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
        writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(token);
        readLoop = ReadEventsAsync();
        await ready.Task.WaitAsync(cancellationToken);
    }

    private async Task SendAsync(PlayerRequest request, CancellationToken cancellationToken)
    {
        if (writer is null) throw new InvalidOperationException("El panel todavía no está conectado.");
        await sendLock.WaitAsync(cancellationToken);
        try { await writer.WriteLineAsync(PlayerProtocol.Serialize(request).AsMemory(), cancellationToken); }
        finally { sendLock.Release(); }
    }

    private async Task ReadEventsAsync()
    {
        try
        {
            while (await reader!.ReadLineAsync() is { } line)
                switch (PlayerProtocol.DeserializeEvent(line))
                {
                    case PlayerEvent.Ready: ready.TrySetResult(); break;
                    case PlayerEvent.SnapshotChanged snapshot: await snapshots.Writer.WriteAsync(snapshot.Value); break;
                    case PlayerEvent.FrameCaptured captured:
                        TaskCompletionSource<VideoFrame>? completion;
                        lock (frameLock) frameRequests.TryGetValue(captured.RequestId, out completion);
                        completion?.TrySetResult(captured.Frame);
                        break;
                    case PlayerEvent.Failed failure when failure.RequestId is { } id:
                        TaskCompletionSource<VideoFrame>? failed;
                        lock (frameLock) frameRequests.TryGetValue(id, out failed);
                        failed?.TrySetException(new InvalidOperationException(failure.Message));
                        break;
                    case PlayerEvent.Failed failure: throw new InvalidOperationException(failure.Message);
                }
        }
        catch (Exception error)
        {
            ready.TrySetException(error);
            snapshots.Writer.TryComplete(error);
            lock (frameLock)
                foreach (var completion in frameRequests.Values) completion.TrySetException(error);
        }
    }

    private long NextId() => Interlocked.Increment(ref requestId);
}
