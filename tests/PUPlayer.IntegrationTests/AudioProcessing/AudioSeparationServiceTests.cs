using PUPlayer.App.AudioProcessing;
using PUPlayer.Core.AudioCache;
using System.IO;

namespace PUPlayer.IntegrationTests.AudioProcessing;

public sealed class AudioSeparationServiceTests
{
    [Fact]
    public async Task CacheMissProcessesOnceAndCacheHitProcessesZeroTimes()
    {
        using var files = new TestFiles();
        var runner = new FakeRunner(command =>
        {
            var output = command.Arguments[command.Arguments.IndexOf("-o") + 1];
            var stems = Path.Combine(output, "htdemucs", "source");
            Directory.CreateDirectory(stems);
            File.WriteAllBytes(Path.Combine(stems, "vocals.wav"), [1, 2, 3]);
            return new(0, "", "");
        });
        var service = files.CreateService(runner);

        var first = await service.GetOrCreateVoiceCacheAsync(files.Source, null, default);
        var second = await service.GetOrCreateVoiceCacheAsync(files.Source, null, default);

        Assert.Equal(first, second);
        Assert.True(File.Exists(first));
        Assert.Single(runner.Commands);
        Assert.EndsWith(".wav", runner.Commands[0].Arguments[^1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CanceledJobRemovesWorkingDirectory()
    {
        using var files = new TestFiles();
        var runner = new FakeRunner(_ => throw new OperationCanceledException());
        var service = files.CreateService(runner);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetOrCreateVoiceCacheAsync(files.Source, null, default));

        Assert.Empty(Directory.EnumerateFileSystemEntries(files.Jobs));
    }

    [Fact]
    public async Task CudaFailureRetriesOnCpu()
    {
        using var files = new TestFiles();
        var runner = new FakeRunner(command =>
        {
            if (command.Arguments.Contains("cuda")) return new(1, "", "CUDA out of memory");
            var output = command.Arguments[command.Arguments.IndexOf("-o") + 1];
            var stems = Path.Combine(output, "htdemucs", "source");
            Directory.CreateDirectory(stems);
            File.WriteAllBytes(Path.Combine(stems, "vocals.wav"), [1]);
            return new(0, "", "");
        });

        await files.CreateService(runner).GetOrCreateVoiceCacheAsync(files.Source, null, default);

        Assert.Equal(2, runner.Commands.Count);
        Assert.Contains("cuda", runner.Commands[0].Arguments);
        Assert.Contains("cpu", runner.Commands[1].Arguments);
    }

    [Fact]
    public async Task NonCudaFailureDoesNotRetryOnCpu()
    {
        using var files = new TestFiles();
        var runner = new FakeRunner(_ => new(1, "Invalid model", ""));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            files.CreateService(runner).GetOrCreateVoiceCacheAsync(files.Source, null, default));

        Assert.Single(runner.Commands);
    }

    [Fact]
    public async Task DetailCache_IsDistinctAndReused()
    {
        using var files = new TestFiles();
        var runner = new FakeRunner(command =>
        {
            var output = command.Arguments[command.Arguments.IndexOf("-o") + 1];
            var stems = Path.Combine(output, "htdemucs", "source");
            Directory.CreateDirectory(stems);
            File.WriteAllBytes(Path.Combine(stems, "vocals.wav"), [1]);
            return new(0, "", "");
        });
        var encoder = new FakeEncoder();
        var service = files.CreateService(runner, encoder);

        var detail = await service.GetOrCreateDetailCacheAsync(files.Source, null, default);
        var cached = await service.GetOrCreateDetailCacheAsync(files.Source, null, default);
        var voice = await service.GetOrCreateVoiceCacheAsync(files.Source, null, default);

        Assert.Equal(detail, cached);
        Assert.NotEqual(detail, voice);
        Assert.Equal(1, encoder.MixCalls);
    }

    private sealed class TestFiles : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "PUPlayer-tests-" + Guid.NewGuid().ToString("N"));
        public string Source => Path.Combine(Root, "source.mp4");
        public string Jobs => Path.Combine(Root, "jobs");

        public TestFiles()
        {
            Directory.CreateDirectory(Jobs);
            File.WriteAllBytes(Source, [1, 2, 3, 4]);
        }

        public AudioSeparationService CreateService(IProcessRunner runner, IAudioEncoder? encoder = null) => new(
            new AudioCacheLocator(Path.Combine(Root, "cache")), runner, encoder ?? new FakeEncoder(),
            Path.Combine(Root, "python.exe"), Jobs);

        public void Dispose() => Directory.Delete(Root, true);
    }

    private sealed class FakeRunner(Func<ProcessCommand, ProcessResult> run) : IProcessRunner
    {
        public List<ProcessCommand> Commands { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromResult(run(command));
        }
    }

    private sealed class FakeEncoder : IAudioEncoder
    {
        public int MixCalls { get; private set; }
        public Task ExtractWavAsync(string sourceMedia, string outputWav, CancellationToken cancellationToken)
        {
            File.WriteAllBytes(outputWav, [1]);
            return Task.CompletedTask;
        }

        public Task EncodeAsync(string inputWav, string outputMka, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputMka)!);
            File.WriteAllBytes(outputMka, [4, 5, 6]);
            return Task.CompletedTask;
        }

        public Task MixDetailAsync(string vocalsWav, string originalWav, string outputMka, CancellationToken cancellationToken)
        {
            MixCalls++;
            return EncodeAsync(vocalsWav, outputMka, cancellationToken);
        }
    }
}

file static class ListExtensions
{
    public static int IndexOf(this IReadOnlyList<string> values, string value)
    {
        for (var i = 0; i < values.Count; i++) if (values[i] == value) return i;
        return -1;
    }
}
