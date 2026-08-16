using System.Diagnostics;
using System.IO;
using System.Text.Json;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Tracking;

namespace PUPlayer.App.Tracking;

public sealed class MediaPipeVisionDetector(string pythonPath, string hostPath, string modelPath) : IVisionDetector
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim gate = new(1, 1);
    private Process? process;
    private long requestId;

    public async Task<IReadOnlyList<PoseCandidate>> DetectAsync(VideoFrame frame, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureStartedAsync(cancellationToken);
            var id = Interlocked.Increment(ref requestId);
            var request = JsonSerializer.Serialize(new VisionRequest(id, frame.Width, frame.Height, frame.Rgb24), Json);
            await process!.StandardInput.WriteLineAsync(request.AsMemory(), cancellationToken);
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken)
                .AsTask().WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken);
            var response = JsonSerializer.Deserialize<VisionResponse>(line ?? "", Json)
                ?? throw new InvalidDataException("El detector no devolvió datos.");
            if (response.Id != id) throw new InvalidDataException("Respuesta visual fuera de orden.");
            if (!string.IsNullOrWhiteSpace(response.Error)) throw new InvalidOperationException(response.Error);
            return response.Poses.Select(points => new PoseCandidate(points.Select(p => new PoseLandmark(p.X, p.Y, p.Visibility)).ToArray())).ToArray();
        }
        catch { Stop(); throw; }
        finally { gate.Release(); }
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (process is { HasExited: false }) return;
        var info = new ProcessStartInfo(pythonPath)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        info.ArgumentList.Add(hostPath);
        info.ArgumentList.Add(modelPath);
        info.Environment["PYTHONUNBUFFERED"] = "1";
        info.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        process = Process.Start(info) ?? throw new InvalidOperationException("No se pudo iniciar el detector visual.");
        var line = await process.StandardOutput.ReadLineAsync(cancellationToken)
            .AsTask().WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        if (JsonSerializer.Deserialize<ReadyResponse>(line ?? "", Json)?.Ready != true)
            throw new InvalidDataException("El detector visual no inició correctamente.");
    }

    private void Stop()
    {
        if (process is null) return;
        if (!process.HasExited) process.Kill(true);
        process.Dispose();
        process = null;
    }

    private sealed record VisionRequest(long Id, int Width, int Height, byte[] Rgb);
    private sealed record VisionPoint(double X, double Y, double Visibility);
    private sealed record VisionResponse(long Id, VisionPoint[][] Poses, string? Error = null);
    private sealed record ReadyResponse(bool Ready);
}
