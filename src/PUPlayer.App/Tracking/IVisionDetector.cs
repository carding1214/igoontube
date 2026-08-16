using PUPlayer.Core.Playback;
using PUPlayer.Core.Tracking;

namespace PUPlayer.App.Tracking;

public interface IVisionDetector : IAsyncDisposable
{
    Task<IReadOnlyList<PoseCandidate>> DetectAsync(VideoFrame frame, CancellationToken cancellationToken);
}
