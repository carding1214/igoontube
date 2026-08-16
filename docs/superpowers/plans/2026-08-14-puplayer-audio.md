# PUPlayer Audio Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir ajustes DSP independientes, separación local de voz con GPU y caché Opus pequeña que se reutiliza sin duplicar el video.

**Architecture:** Los ajustes inmediatos se traducen a filtros libavfilter dentro del worker mpv de cada panel. La separación pesada se ejecuta en un proceso local Demucs administrado por la app; el resultado vocal temporal se codifica como `.mka` Opus y mpv lo carga como pista externa sincronizada.

**Tech Stack:** .NET 8, WPF, libmpv, FFmpeg 8, uv, Python 3.11, PyTorch 2.1.2 CUDA 12.1, Demucs 4.0.1, xUnit.

## Global Constraints

- Solo archivos locales; ninguna URL entra al protocolo.
- Volumen por panel entre 0 y 200 %.
- IA, modelos, paquetes, temporales y cachés viven en F: salvo caché adyacente a un video que ya esté fuera de C:.
- La caché persistente contiene solo audio `.mka` Opus a 64 kbit/s; los WAV temporales se eliminan al terminar o cancelar.
- Una separación simultánea por aplicación; la reproducción nunca se bloquea.
- El modelo inicial es `htdemucs`, dos stems `vocals`; GPU CUDA con fallback CPU.

---

### Task 1: DSP presets and manual audio settings

**Files:**
- Create: `src/PUPlayer.Core/Audio/AudioPreset.cs`
- Create: `src/PUPlayer.Core/Audio/AudioSettings.cs`
- Create: `src/PUPlayer.Core/Audio/MpvAudioFilterBuilder.cs`
- Test: `tests/PUPlayer.Core.Tests/Audio/MpvAudioFilterBuilderTests.cs`

**Interfaces:**
- Consumes: preset plus manual low-cut, voice gain, presence gain, denoise and compression.
- Produces: one safe mpv `af` value; numbers use invariant culture and fixed clamps.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public void VoicePreset_BuildsLocalLavfiChain()
{
    var value = MpvAudioFilterBuilder.Build(AudioSettings.FromPreset(AudioPreset.Voice));
    Assert.Contains("highpass=f=70", value);
    Assert.Contains("equalizer=f=1200", value);
    Assert.DoesNotContain("http", value, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void ManualValues_AreClamped()
{
    var settings = new AudioSettings(999, 99, -99, 2, true);
    Assert.Equal(new AudioSettings(180, 12, -12, 1, true), settings.Clamp());
}
```

- [ ] **Step 2: Run:** `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter MpvAudioFilterBuilderTests`; expect missing types.
- [ ] **Step 3: Implement presets:** `Natural`, `Voice`, `Intimate`, `Denoise`; use `highpass`, two `equalizer` bands, optional `afftdn` and `acompressor`; `Natural` returns an empty filter.
- [ ] **Step 4: Run focused and full tests; expect PASS.**
- [ ] **Step 5: Commit:** `git commit -m "feat: add per-panel audio presets"`.

### Task 2: Send DSP and external audio to each mpv worker

**Files:**
- Modify: `src/PUPlayer.Core/Playback/PlayerRequest.cs`
- Modify: `src/PUPlayer.MpvWorker/Interop/IMpvClient.cs`
- Modify: `src/PUPlayer.MpvWorker/Interop/MpvClient.cs`
- Modify: `src/PUPlayer.MpvWorker/Worker/PlayerWorker.cs`
- Test: `tests/PUPlayer.Core.Tests/Playback/PlayerProtocolTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Worker/PlayerWorkerTests.cs`

**Interfaces:**
- Produces requests `SetAudioFilter(Id, Value)` and `LoadExternalAudio(Id, Path)`.

- [ ] **Step 1: Add failing round-trip and recording-client tests for both requests.**
- [ ] **Step 2: Run focused tests; expect missing request/client members.**
- [ ] **Step 3: Map filter to `Command("set", "af", value)` and cache to `Command("audio-add", path, "select")`; validate the cache with `LocalMediaPath.TryCreate` again inside the worker.**
- [ ] **Step 4: Run worker self-test and full suite; expect PASS.**
- [ ] **Step 5: Commit:** `git commit -m "feat: control panel audio filters"`.

### Task 3: Per-panel audio UI

**Files:**
- Modify: `src/PUPlayer.App/Playback/IPlayerBackend.cs`
- Modify: `src/PUPlayer.App/Playback/MpvWorkerBackend.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces `ApplyAudioAsync(AudioSettings)` and independent observable settings per panel.

- [ ] **Step 1: Add a failing test that changes the left preset and asserts the right backend receives no call.**
- [ ] **Step 2: Run focused tests; expect missing audio methods.**
- [ ] **Step 3: Add preset selector plus expandable manual sliders: low cut 0–180 Hz, voice/presence -12–12 dB, denoise 0–1, compressor toggle. Apply only after slider release.**
- [ ] **Step 4: Run tests and inspect one/two-panel layouts at 1180×720; expect no clipped controls.**
- [ ] **Step 5: Commit:** `git commit -m "feat: expose independent audio tuning"`.

### Task 4: Stable small cache paths

**Files:**
- Create: `src/PUPlayer.Core/AudioCache/AudioCacheKey.cs`
- Create: `src/PUPlayer.Core/AudioCache/AudioCacheLocator.cs`
- Create: `src/PUPlayer.Core/AudioCache/AudioCacheManifest.cs`
- Test: `tests/PUPlayer.Core.Tests/AudioCache/AudioCacheLocatorTests.cs`

**Interfaces:**
- Consumes: source full path, length, last-write UTC and model id.
- Produces: `<source>.pucache/voice-<hash>.mka`, or `F:\PUPlayer\data\cache\<hash>\voice.mka` when the source drive is C: or adjacent writes fail.

- [ ] **Step 1: Write failing deterministic-key, invalidation and C:-fallback tests.**
- [ ] **Step 2: Run focused tests; expect missing types.**
- [ ] **Step 3: Hash normalized metadata with SHA-256; write manifest atomically beside the final MKA; accept a cache only when source metadata, model and file size match.**
- [ ] **Step 4: Run focused/full tests; expect PASS.**
- [ ] **Step 5: Commit:** `git commit -m "feat: locate compact AI audio caches"`.

### Task 5: FFmpeg cache encoder

**Files:**
- Create: `src/PUPlayer.App/AudioProcessing/ProcessRunner.cs`
- Create: `src/PUPlayer.App/AudioProcessing/FfmpegAudioEncoder.cs`
- Test: `tests/PUPlayer.IntegrationTests/AudioProcessing/FfmpegAudioEncoderTests.cs`

**Interfaces:**
- Consumes: temporary `vocals.wav`, final cache path, cancellation token.
- Produces: Matroska audio only, Opus 64 kbit/s VBR, written to `.partial` then atomically renamed.

- [ ] **Step 1: Generate a short WAV and write a failing test that probes output with `ffmpeg -i`; assert one audio stream, zero video streams and output below 200 KB.**
- [ ] **Step 2: Run focused test; expect missing encoder.**
- [ ] **Step 3: Run FFmpeg with argument list `-vn -c:a libopus -b:a 64k -vbr on -application audio`; on cancellation kill the process tree and delete `.partial`.**
- [ ] **Step 4: Run focused/full tests; expect PASS.**
- [ ] **Step 5: Commit:** `git commit -m "feat: encode compact Opus caches"`.

### Task 6: Portable Demucs environment on F:

**Files:**
- Create: `scripts/bootstrap-ai.ps1`
- Create: `scripts/verify-ai.ps1`
- Modify: `.gitignore`

**Interfaces:**
- Produces: `.tools/uv/uv.exe`, `.tools/ai/.venv/Scripts/python.exe`; model cache under `data/models`.

- [ ] **Step 1: Write `verify-ai.ps1` first; require Python 3.11, `demucs==4.0.1`, `torch==2.1.2`, `torchaudio==2.1.2` and successful `torch.cuda.is_available()` or explicit CPU fallback.**
- [ ] **Step 2: Run verification; expect missing uv/environment.**
- [ ] **Step 3: Download official uv Windows x64 release; set `UV_CACHE_DIR`, `UV_PYTHON_INSTALL_DIR`, `UV_PYTHON_BIN_DIR`, `TEMP`, `TMP` and `TORCH_HOME` under repository root; install Python 3.11, PyTorch CUDA 12.1 wheels, Demucs 4.0.1, SoundFile, `numpy<2` and `packaging`.**
- [ ] **Step 4: Run verification and a 2-second `--two-stems=vocals -n htdemucs` smoke; expect two WAV stems under `work`.**
- [ ] **Step 5: Commit:** `git commit -m "build: bootstrap local Demucs runtime"`.

### Task 7: Separation, progress, cancellation and cache reuse

**Files:**
- Create: `src/PUPlayer.App/AudioProcessing/AudioSeparationService.cs`
- Create: `src/PUPlayer.App/AudioProcessing/AudioProcessingProgress.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Test: `tests/PUPlayer.IntegrationTests/AudioProcessing/AudioSeparationServiceTests.cs`

**Interfaces:**
- Produces: `GetOrCreateVoiceCacheAsync(source, progress, cancellationToken)` and cache hit without spawning Demucs.

- [x] **Step 1: Add failing tests with fake process runner: one process on miss, zero on hit, `.partial` removed on cancel.**
- [x] **Step 2: Run focused tests; expect missing service.**
- [x] **Step 3: Serialize jobs with `SemaphoreSlim(1,1)`; extract a temporary WAV, invoke Demucs with CUDA and retry once with CPU only on a CUDA failure. Encode vocals, write the manifest, delete the job directory and load the MKA through that panel backend.**
- [x] **Step 4: Add UI buttons `Mejorar voz con IA` / `Cancelar`, progress text, cache-hit text and `Usar audio original`; run focused/full tests.**
- [x] **Step 5: Commit:** `git commit -m "feat: cache local voice separation"`.

### Task 8: Audio verification and handoff

**Files:**
- Create: `docs/manual-tests/audio-smoke.md`
- Modify: `docs/superpowers/plans/2026-08-13-puplayer-roadmap.md`

**Interfaces:**
- Verifies two simultaneous panels, independent DSP, AI cache reuse and privacy.

- [x] **Step 1: Add a smoke test that proves a second request reuses the same MKA and does not start Python.**
- [x] **Step 2: Run all Release tests, direct libmpv self-test and `verify-ai.ps1`.**
- [x] **Step 3: Inspect cache with FFmpeg: one Opus audio stream, no video; record size for a generated 10-minute source.**
- [x] **Step 4: Confirm zero residual Python/FFmpeg/PUPlayer processes and no writes under C: from the test run.**
- [x] **Step 5: Commit:** `git commit -m "test: verify independent AI audio cache"`.
