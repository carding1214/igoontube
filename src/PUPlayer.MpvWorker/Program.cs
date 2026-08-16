using System.IO.Pipes;
using System.Text;
using PUPlayer.Core.Playback;
using PUPlayer.MpvWorker.Interop;
using PUPlayer.MpvWorker.Worker;

namespace PUPlayer.MpvWorker;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args is ["--self-test-mpv", var path]) return SelfTest(path);
        if (!WorkerOptions.TryParse(args, out var options)) return 2;

        using var pipe = new NamedPipeServerStream(options!.PipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        await pipe.WaitForConnectionAsync();
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        if (!string.Equals(await reader.ReadLineAsync(), options.Token, StringComparison.Ordinal)) return 3;

        using var worker = new PlayerWorker(new MpvClient(options.WindowId, options.MpvPath));
        await writer.WriteLineAsync(PlayerProtocol.Serialize<PlayerEvent>(new PlayerEvent.Ready()));
        var read = reader.ReadLineAsync();
        while (!worker.IsShutdown)
        {
            var tick = Task.Delay(250);
            if (await Task.WhenAny(read, tick) == tick)
            {
                await writer.WriteLineAsync(PlayerProtocol.Serialize<PlayerEvent>(new PlayerEvent.SnapshotChanged(worker.ReadSnapshot())));
                continue;
            }

            var line = await read;
            if (line is null) break;
            PlayerRequest? request = null;
            try
            {
                request = PlayerProtocol.DeserializeRequest(line);
                var response = await worker.ApplyAsync(request, default);
                if (response is not null) await writer.WriteLineAsync(PlayerProtocol.Serialize(response));
            }
            catch (Exception error)
            {
                await writer.WriteLineAsync(PlayerProtocol.Serialize<PlayerEvent>(new PlayerEvent.Failed("request", error.Message, request?.Id)));
            }
            read = reader.ReadLineAsync();
        }
        return 0;
    }

    private static int SelfTest(string path)
    {
        MpvLibraryResolver.Register(Path.GetFullPath(path));
        var context = MpvNative.mpv_create();
        if (context == nint.Zero) return 1;
        MpvNative.mpv_terminate_destroy(context);
        Console.WriteLine("libmpv: ok");
        return 0;
    }
}
