using PUPlayer.Core.MediaTools;

namespace PUPlayer.App.MediaTools;

public enum ClipExportMode { Original, CurrentView }
public sealed record ClipExportRequest(string Source, ClipSelection Selection, string Output, ClipExportMode Mode, VideoTransform Transform);
public sealed record ClipExportProgress(double Fraction, string Message);

public interface IClipExportService
{
    Task<string> ExportAsync(ClipExportRequest request, IProgress<ClipExportProgress>? progress, CancellationToken cancellationToken);
}
