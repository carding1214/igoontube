# PUPlayer Intimate Audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir un preset inmediato y una caché IA que preserve voz, vocalizaciones y detalles cercanos con música reducida.

**Architecture:** El preset amplía el DSP existente. La ruta IA reutiliza Demucs y mezcla `vocals.wav` con 22% del WAV original mediante FFmpeg antes de codificar Opus; usa una clave de caché propia.

**Tech Stack:** .NET 8, FFmpeg, Demucs, xUnit, WPF.

## Global Constraints

- Todo el procesamiento permanece local.
- La caché contiene solo audio `.mka`, nunca video.
- Los ajustes y procesos siguen siendo independientes por panel.
- La salida debe limitar picos para evitar saturación.

---

### Task 1: Preset `DetailedIntimate`

**Files:**
- Modify: `src/PUPlayer.Core/Audio/AudioPreset.cs`
- Modify: `src/PUPlayer.Core/Audio/AudioSettings.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Test: `tests/PUPlayer.Core.Tests/Audio/MpvAudioFilterBuilderTests.cs`

**Interfaces:**
- Produces: `AudioPreset.DetailedIntimate` mapped to `AudioSettings(35, 4, 6, 0, true)`.

- [x] Add a failing test that asserts low cut 35 Hz, +4 dB voice, +6 dB presence, no denoise and compression.
- [x] Run the focused Core test and confirm failure.
- [x] Add the enum value, mapping and combo-box option `Íntimo detallado`.
- [x] Run all Core tests and commit `feat: add detailed intimate preset`.

### Task 2: FFmpeg intimate mix

**Files:**
- Modify: `src/PUPlayer.App/AudioProcessing/FfmpegAudioEncoder.cs`
- Test: `tests/PUPlayer.IntegrationTests/AudioProcessing/FfmpegAudioEncoderTests.cs`

**Interfaces:**
- Produces: `Task MixDetailAsync(string vocalsWav, string originalWav, string outputMka, CancellationToken)` on `IAudioEncoder`.

- [x] Add a failing test with two generated WAV inputs and assert a valid, audio-only Opus MKA.
- [x] Confirm the interface and implementation are missing.
- [x] Implement FFmpeg `filter_complex`: vocal high-pass/presence/gain, original `volume=0.22`, `amix`, compressor and limiter; encode Opus at 80 kb/s atomically.
- [x] Run encoder tests and commit `feat: preserve intimate audio details`.

### Task 3: Independent IA cache and panel action

**Files:**
- Modify: `src/PUPlayer.App/AudioProcessing/AudioSeparationService.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Test: `tests/PUPlayer.IntegrationTests/AudioProcessing/AudioSeparationServiceTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces: `IAudioSeparationService.GetOrCreateDetailCacheAsync(...)` with model id `htdemucs-4.0.1-intimate-detail-v1`.

- [x] Add failing tests for distinct cache keys, reuse, panel-local loading, cancellation and original-audio restoration.
- [x] Confirm RED.
- [x] Reuse the current CUDA/CPU Demucs flow, call `MixDetailAsync`, and expose `EnhanceDetailAsync`.
- [x] Add `Mejorar detalle con IA` to the existing compact popup.
- [x] Run integration tests and commit `feat: add intimate AI audio cache`.

### Task 4: Real-file verification and delivery

**Files:**
- Modify: `docs/manual-tests/audio-smoke.md`
- Modify: `scripts/publish-portable.ps1`

**Interfaces:**
- Verifies the user-provided MP4 without retaining test video copies.

- [x] Extract a short temporary audio/video segment from the supplied file under `work`, test presets, volume, zoom, mosaic, tracking and both IA modes, then delete the segment.
- [x] Remove Python caches, tests, headers and build-only files from the portable package; verify imports, CUDA and MediaPipe afterward.
- [x] Run Release tests, portable self-tests and two-window launch.
- [x] Rebuild installer and retain editable source at `F:\PUPlayer\.worktrees\core`.
- [x] Commit `test: verify intimate audio delivery`.
