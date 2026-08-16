# IgoonTube NoAI Distribution Design

## Goal

Ship a much smaller optional build that keeps the complete local player and media tools but contains no Python, PyTorch, Demucs, MediaPipe or AI models.

## Variants

- `IgoonTube`: full application with local AI audio, body tracking and automatic scene analysis.
- `IgoonTube-NoAI`: lightweight application without those three AI-dependent features.

Both variants keep independent windows, two-panel layouts, fullscreen, timelines, volume to 200%, manual audio presets, zoom, rotation, mirrors, crop, favorites, thumbnails, cache management and clip export.

## Build boundary

MSBuild property `NoAI=true` defines `IGOONTUBE_NO_AI`. The lightweight build does not create AI feature factories and exposes `AiFeaturesAvailable=false` to the UI. AI audio controls, tracking and automatic scene analysis are hidden rather than left disabled or broken.

FFmpeg and libmpv remain because playback, thumbnails, clips and non-AI audio controls use them.

## Packaging

Create dedicated scripts and outputs:

- Portable: `F:\PUPlayer\dist\IgoonTube-NoAI\IgoonTube.exe`
- Installer: `F:\PUPlayer\dist\IgoonTube-NoAI-Setup.exe`
- Optional installer data parts retain the `IgoonTube-NoAI-Setup-*.bin` prefix.

The portable package must exclude `.tools\ai`, AI model folders and `scripts\vision_host.py`. It includes a NoAI-specific readme. The existing full package and installer remain unchanged.

The NoAI installer uses a distinct AppId so Windows can install both variants simultaneously. File associations remain optional and use the NoAI executable only when the user selects them.

## Runtime behavior

NoAI startup must not probe for Python or AI models. Its ordinary and optional non-AI functions remain lazy. Settings are compatible with the full version, but selecting the NoAI build never exposes controls it cannot execute.

## Verification

- Compile and test both `NoAI=false` and `NoAI=true`.
- Assert AI controls are absent in the NoAI UI state.
- Inspect the portable tree to prove Python, models and vision host are absent.
- Play the supplied real video, seek, change manual audio, generate a thumbnail and export a short clip.
- Rebuild source ZIP and SHA-256 hashes for all final artifacts.
