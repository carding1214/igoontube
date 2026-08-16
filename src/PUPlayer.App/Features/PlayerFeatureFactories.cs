using PUPlayer.App.AudioProcessing;
using PUPlayer.App.MediaTools;
using PUPlayer.App.Tracking;

namespace PUPlayer.App.Features;

public sealed class PlayerFeatureFactories(
    Func<IAudioSeparationService?> audio,
    Func<IVisionDetector?> vision,
    Func<ISceneAnalysisService?> scenes,
    Func<IClipExportService?> clips,
    Func<IThumbnailService?> thumbnails)
{
    private readonly Lazy<IAudioSeparationService?> audio = New(audio);
    private readonly Lazy<IVisionDetector?> vision = New(vision);
    private readonly Lazy<ISceneAnalysisService?> scenes = New(scenes);
    private readonly Lazy<IClipExportService?> clips = New(clips);
    private readonly Lazy<IThumbnailService?> thumbnails = New(thumbnails);

    public IAudioSeparationService? Audio => audio.Value;
    public IVisionDetector? Vision => vision.Value;
    public ISceneAnalysisService? Scenes => scenes.Value;
    public IClipExportService? Clips => clips.Value;
    public IThumbnailService? Thumbnails => thumbnails.Value;

    private static Lazy<T?> New<T>(Func<T?> factory) where T : class =>
        new(factory, LazyThreadSafetyMode.ExecutionAndPublication);
}
