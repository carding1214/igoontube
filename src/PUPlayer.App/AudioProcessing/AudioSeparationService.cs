using System.IO;
using PUPlayer.Core.AudioCache;

namespace PUPlayer.App.AudioProcessing;

public interface IAudioSeparationService
{
    Task<string> GetOrCreateVoiceCacheAsync(string sourcePath,
        IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken);
    Task<string> GetOrCreateDetailCacheAsync(string sourcePath,
        IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken);
}

public sealed class AudioSeparationService(
    AudioCacheLocator cache,
    IProcessRunner runner,
    IAudioEncoder encoder,
    string pythonPath,
    string jobsRoot,
    IReadOnlyDictionary<string, string>? environment = null) : IAudioSeparationService
{
    public const string ModelId = "htdemucs-4.0.1-vocals";
    public const string DetailModelId = "htdemucs-4.0.1-intimate-detail-v1";
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task<string> GetOrCreateVoiceCacheAsync(string sourcePath,
        IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken)
        => await GetOrCreateAsync(sourcePath, ModelId, false, progress, cancellationToken);

    public async Task<string> GetOrCreateDetailCacheAsync(string sourcePath,
        IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken)
        => await GetOrCreateAsync(sourcePath, DetailModelId, true, progress, cancellationToken);

    private async Task<string> GetOrCreateAsync(string sourcePath, string modelId, bool detail,
        IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken)
    {
        var location = cache.Locate(AudioCacheKey.FromFile(sourcePath, modelId));
        if (IsValid(location)) return Hit(location, progress);

        progress?.Report(new(AudioProcessingStage.Waiting, "Esperando turno…"));
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (IsValid(location)) return Hit(location, progress);
            return await CreateAsync(location, detail, progress, cancellationToken);
        }
        finally { Gate.Release(); }
    }

    private async Task<string> CreateAsync(AudioCacheLocation location, bool detail,
        IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(jobsRoot);
        var job = Path.Combine(jobsRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(job);
        try
        {
            progress?.Report(new(AudioProcessingStage.Separating, "Separando voz con IA…"));
            var input = Path.Combine(job, "input.wav");
            await encoder.ExtractWavAsync(location.Key.SourcePath, input, cancellationToken);
            var result = await SeparateAsync(input, job, "cuda", cancellationToken);
            if (result.ExitCode != 0 && IsCudaFailure(result))
            {
                progress?.Report(new(AudioProcessingStage.Separating, "Reintentando con CPU…"));
                result = await SeparateAsync(input, job, "cpu", cancellationToken);
            }
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Demucs falló: {(result.StandardError + result.StandardOutput).Trim()}");

            var vocals = Directory.EnumerateFiles(job, "vocals.wav", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidDataException("Demucs no produjo la pista vocal.");
            progress?.Report(new(AudioProcessingStage.Encoding, "Optimizando caché de audio…"));
            if (detail) await encoder.MixDetailAsync(vocals, input, location.AudioPath, cancellationToken);
            else await encoder.EncodeAsync(vocals, location.AudioPath, cancellationToken);
            AudioCacheManifest.From(location.Key, new FileInfo(location.AudioPath).Length).SaveAtomic(location.ManifestPath);
            progress?.Report(new(AudioProcessingStage.Completed, detail ? "Detalle íntimo listo" : "Voz mejorada lista"));
            return location.AudioPath;
        }
        finally
        {
            if (Directory.Exists(job)) Directory.Delete(job, true);
        }
    }

    private Task<ProcessResult> SeparateAsync(string source, string output, string device, CancellationToken token) =>
        runner.RunAsync(new(pythonPath,
        [
            "-m", "demucs.separate", "--two-stems=vocals", "-n", "htdemucs", "--device", device,
            "--segment", "7", "--overlap", "0.1", "-o", output, source
        ], Path.GetDirectoryName(source), environment), token);

    private static bool IsValid(AudioCacheLocation location)
    {
        try { return AudioCacheManifest.Load(location.ManifestPath).Matches(location.Key, location.AudioPath); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { return false; }
    }

    private static string Hit(AudioCacheLocation location, IProgress<AudioProcessingProgress>? progress)
    {
        progress?.Report(new(AudioProcessingStage.Cached, "Caché de voz lista"));
        return location.AudioPath;
    }

    private static bool IsCudaFailure(ProcessResult result)
    {
        var detail = result.StandardError + result.StandardOutput;
        return detail.Contains("cuda", StringComparison.OrdinalIgnoreCase) ||
               detail.Contains("cudnn", StringComparison.OrdinalIgnoreCase) ||
               detail.Contains("gpu", StringComparison.OrdinalIgnoreCase) ||
               detail.Contains("nvidia", StringComparison.OrdinalIgnoreCase);
    }
}
