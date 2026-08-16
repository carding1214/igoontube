# PUPlayer Visual Tracking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir seguimiento local por panel que encuadre cuerpo completo, torso o cara y ceda ante el zoom manual.

**Architecture:** libmpv entrega el fotograma visible en memoria con `screenshot-raw`; el worker lo reduce antes de enviarlo. Un proceso MediaPipe CPU compartido por ventana detecta hasta cuatro poses a 5 FPS. C# selecciona, suaviza y convierte el sujeto en transformaciones mpv independientes.

**Tech Stack:** .NET 8, WPF, libmpv client API, Python 3.11, MediaPipe 0.10.35, xUnit.

## Global Constraints

- Solo memoria: ningún fotograma se guarda.
- Máximo 384 px de ancho y 5 FPS por panel; bajar a 2 FPS si el análisis supera 180 ms.
- Un proceso visual por ventana y una inferencia simultánea.
- Cuerpo completo si tobillos y cabeza son visibles; torso si no; cara como último recurso.
- Pérdida de 2 s: restablecer encuadre completo.
- Zoom o paneo manual detiene el seguimiento de ese panel.

---

### Task 1: Tracking geometry and smoothing

**Files:**
- Create: `src/PUPlayer.Core/Tracking/NormalizedBox.cs`
- Create: `src/PUPlayer.Core/Tracking/PoseCandidate.cs`
- Create: `src/PUPlayer.Core/Tracking/AutoFrameTracker.cs`
- Test: `tests/PUPlayer.Core.Tests/Tracking/AutoFrameTrackerTests.cs`

**Interfaces:**
- Consumes: normalized MediaPipe landmarks, timestamp and optional click point.
- Produces: `MpvTransform? Update(IReadOnlyList<PoseCandidate>, TimeSpan)` plus selection/loss state.

- [ ] **Step 1:** Write failing tests for full-body preference, torso/face fallback, nearest-click selection, smoothing and reset after two seconds.
- [ ] **Step 2:** Run the focused tests and confirm missing tracking types.
- [ ] **Step 3:** Implement clamped boxes, 15% margin, nearest-center identity matching and exponential smoothing (`alpha=0.22`).
- [ ] **Step 4:** Run focused and full Core tests.
- [ ] **Step 5:** Commit `feat: add subject auto framing`.

### Task 2: In-memory frame capture from libmpv

**Files:**
- Modify: `src/PUPlayer.Core/Playback/PlayerRequest.cs`
- Modify: `src/PUPlayer.Core/Playback/PlayerEvent.cs`
- Create: `src/PUPlayer.Core/Playback/VideoFrame.cs`
- Modify: `src/PUPlayer.MpvWorker/Interop/MpvNative.cs`
- Modify: `src/PUPlayer.MpvWorker/Interop/IMpvClient.cs`
- Modify: `src/PUPlayer.MpvWorker/Interop/MpvClient.cs`
- Modify: `src/PUPlayer.MpvWorker/Worker/PlayerWorker.cs`
- Modify: `src/PUPlayer.MpvWorker/Program.cs`
- Modify: `src/PUPlayer.App/Playback/IPlayerBackend.cs`
- Modify: `src/PUPlayer.App/Playback/MpvWorkerBackend.cs`
- Test: `tests/PUPlayer.Core.Tests/Playback/PlayerProtocolTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Worker/PlayerWorkerTests.cs`

**Interfaces:**
- Produces: `Task<VideoFrame?> CaptureFrameAsync(CancellationToken)`; RGB24, width, height, timestamp.

- [ ] **Step 1:** Add failing protocol and worker tests for `CaptureFrame` / `FrameCaptured` correlation.
- [ ] **Step 2:** Confirm RED.
- [ ] **Step 3:** Bind `mpv_command_node`, parse the returned node map, copy `bgr0`, reduce to at most 384 px and convert to RGB24 before freeing the node.
- [ ] **Step 4:** Correlate frame events with pending requests in `MpvWorkerBackend`; timeout after 300 ms.
- [ ] **Step 5:** Run tests and direct libmpv capture smoke; commit `feat: capture frames in memory`.

### Task 3: Portable MediaPipe vision host

**Files:**
- Create: `scripts/vision_host.py`
- Create: `src/PUPlayer.App/Tracking/IVisionDetector.cs`
- Create: `src/PUPlayer.App/Tracking/MediaPipeVisionDetector.cs`
- Modify: `scripts/bootstrap-ai.ps1`
- Modify: `scripts/verify-ai.ps1`
- Test: `tests/PUPlayer.IntegrationTests/Tracking/MediaPipeVisionDetectorTests.cs`

**Interfaces:**
- Consumes: one RGB24 `VideoFrame` as base64 JSON line.
- Produces: normalized landmarks for at most four poses as one JSON line with the same request id.

- [ ] **Step 1:** Write a failing process-contract test using a deterministic fake host.
- [ ] **Step 2:** Confirm RED.
- [ ] **Step 3:** Implement one long-lived process, serialized stdin/stdout, 500 ms timeout, cancellation and process-tree cleanup.
- [ ] **Step 4:** Add MediaPipe 0.10.35 and the official pose-landmarker-lite model under `data/models/mediapipe`; implement image-mode detection without masks or image writes.
- [ ] **Step 5:** Verify package/model and a synthetic-image smoke; commit `feat: detect local pose landmarks`.

### Task 4: Independent tracking loop and controls

**Files:**
- Create: `src/PUPlayer.App/Tracking/VisionCoordinator.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Playback/MpvSurface.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces: `StartTrackingAsync`, `StopTrackingAsync`, `SelectSubjectAsync(NormalizedPoint)` and status properties.

- [ ] **Step 1:** Add failing tests proving tracking changes only its panel, multiple poses wait for a click, manual wheel/pan stops tracking and two-second loss resets zoom.
- [ ] **Step 2:** Confirm RED.
- [ ] **Step 3:** Implement a cancellation-safe 5 FPS loop with one inference gate and adaptive 2 FPS fallback.
- [ ] **Step 4:** Add a compact `Seguir` toggle to the existing one-line strip; show `Haz clic en una persona`, `Siguiendo cuerpo/torso/cara` or a concise error. Video clicks select the nearest candidate.
- [ ] **Step 5:** Run full tests and inspect one/two-panel layouts; commit `feat: follow subjects per video`.

### Task 5: Vision verification

**Files:**
- Create: `docs/manual-tests/vision-smoke.md`
- Modify: `docs/superpowers/plans/2026-08-13-puplayer-roadmap.md`

**Interfaces:**
- Verifies privacy, loss recovery, layout independence and no residual processes.

- [ ] **Step 1:** Run Release tests, libmpv self-test and `verify-ai.ps1`.
- [ ] **Step 2:** Test one-person, two-person and temporary-loss clips; confirm body/torso/face fallback.
- [ ] **Step 3:** Confirm manual zoom stops only that panel and layout changes preserve the other panel.
- [ ] **Step 4:** Confirm no frame files and no residual vision processes.
- [ ] **Step 5:** Commit `test: verify local subject tracking`.
