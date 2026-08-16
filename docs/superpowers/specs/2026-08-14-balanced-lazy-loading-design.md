# Balanced Lazy Loading Design

## Goal

Show every independent IgoonTube window immediately, prioritize the first playable frame, and retain smooth seeking without starting optional processing.

Targets on a local SSD are a visible window within 500 ms and a first frame within 1–2 seconds. Hardware and media format may increase the latter.

## Startup path

1. Create the window, controls and one player panel.
2. Show a compact `Loading…` indicator over the video.
3. Start that panel's mpv worker asynchronously with hardware decoding and moderate local-file read-ahead.
4. Remove the indicator when the player reports usable media state.
5. Load favorites and secondary metadata after playback has started.

Each panel and real window loads independently. Opening a second video never waits for another panel.

## Deferred work

The following must not run or scan during ordinary opening:

- Global and per-video cache enumeration.
- Thumbnail generation.
- FFmpeg probing, scene analysis and clip keyframe scanning.
- Python, PyTorch, Demucs or MediaPipe process startup.
- AI audio validation beyond a direct cache lookup requested by the user.

Factories create these services only when their corresponding control is used. Existing results remain reusable.

Favorites use a lightweight asynchronous read after the first playable state. Their failure does not interrupt playback.

## Playback balance

mpv retains `hwdec=auto-safe`. Local playback uses bounded, moderate read-ahead: enough for responsive seeking without scanning or buffering the whole file. Network options remain irrelevant because only validated local paths are accepted.

The loading overlay is non-modal and does not resize the video. After ten seconds it changes to `Still loading…`; the window remains closable. A player-load failure replaces it with a concise error. Optional-feature failures are isolated from playback.

## Instrumentation

Record lightweight in-memory timestamps for:

- Application startup to visible window.
- Visible window to worker ready.
- Worker ready to first usable media snapshot.

Diagnostics are local, disabled from persistent logging by default, and contain no media paths.

## Tests

- Unit tests prove optional factories are not invoked during construction/opening.
- Integration tests prove no Python, FFmpeg or MediaPipe process is started by normal playback.
- Loading-state tests cover success, timeout text, failure and independent two-panel loading.
- Benchmark smoke tests use a short fixture and the supplied large local video when available.
- Existing playback, AI, cache, media-tool and distribution suites remain green.

## Acceptance

- The window is responsive while video loads.
- Normal opening performs no optional processing or cache-tree scan.
- First-frame time does not regress and seeking remains smooth.
- Every optional function initializes on first use and continues using its cache.
