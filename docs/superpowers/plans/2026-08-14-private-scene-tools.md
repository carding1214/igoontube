# Private Scene Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir análisis discreto de escenas, favoritos, bucle A–B y modo privado global a IgoonTube.

**Architecture:** Un analizador local reutiliza Demucs y FFmpeg y persiste un índice JSON por video. El ViewModel de cada panel posee marcadores y bucle; MainWindow coordina únicamente el atajo global de privacidad.

**Tech Stack:** .NET 8, WPF, FFmpeg, Demucs, JSON, xUnit.

## Global Constraints

- Ejecución bajo demanda y completamente local.
- Etiquetas neutrales: `Voz`, `Detalle`, `Actividad alta`.
- El índice no contiene audio, video ni miniaturas.
- Un análisis simultáneo como máximo.
- `Ctrl+Shift+M` minimiza, silencia y oculta nombres; restaurar no recupera volumen.

---

### Task 1: Modelo e índice persistente

**Files:**
- Create: `src/PUPlayer.Core/Scenes/SceneMarker.cs`
- Create: `src/PUPlayer.Core/Scenes/SceneIndex.cs`
- Create: `src/PUPlayer.Core/Scenes/SceneIndexStore.cs`
- Create: `tests/PUPlayer.Core.Tests/Scenes/SceneIndexStoreTests.cs`

**Interfaces:**
- Produces: `SceneMarker(double Seconds, SceneMarkerKind Kind, string Label)` y `SceneIndexStore.Load/Save`.

- [ ] Escribir pruebas de serialización, clave, invalidación y JSON corrupto; confirmar RED.
- [ ] Implementar almacenamiento atómico en `.pucache`; confirmar GREEN.
- [ ] Commit: `feat: persist private scene markers`.

### Task 2: Analizador híbrido bajo demanda

**Files:**
- Create: `src/PUPlayer.App/SceneAnalysis/SceneAnalysisService.cs`
- Modify: `src/PUPlayer.App/AudioProcessing/FfmpegAudioEncoder.cs`
- Create: `tests/PUPlayer.IntegrationTests/SceneAnalysis/SceneAnalysisServiceTests.cs`

**Interfaces:**
- Produces: `AnalyzeAsync(string mediaPath, double sensitivity, IProgress<double>, CancellationToken)`.

- [ ] Generar un audio sintético y escribir pruebas de marcas, cancelación y reutilización; confirmar RED.
- [ ] Extraer una pista temporal mono de baja tasa, reutilizar separación vocal y calcular ventanas RMS/transitorios.
- [ ] Etiquetar sin inferencias explícitas, guardar JSON y borrar temporales incluso al cancelar.
- [ ] Confirmar GREEN y commit `feat: analyze private scenes locally`.

### Task 3: Marcadores, favoritos y bucle A–B

**Files:**
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces: comandos de analizar/cancelar, sensibilidad, marcadores, favoritos y A/B por panel.

- [ ] Escribir pruebas de aislamiento, salto, validación A/B y repetición; confirmar RED.
- [ ] Implementar estado y salto B→A desde eventos de posición.
- [ ] Dibujar marcas clicables sobre la línea de tiempo y colocar acciones secundarias en el popup compacto.
- [ ] Confirmar GREEN y commit `feat: add scene navigation and AB loop`.

### Task 4: Atajo de privacidad

**Files:**
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Workspace/WorkspaceViewModelTests.cs`

**Interfaces:**
- Produces: `WorkspaceViewModel.ActivatePrivacyAsync()` y `IsPrivate`.

- [ ] Escribir pruebas de silencio, nombres ocultos, dos paneles y volumen no restaurado; confirmar RED.
- [ ] Implementar privacidad y conectar `Ctrl+Shift+M` antes de minimizar.
- [ ] Confirmar GREEN y commit `feat: add discreet privacy shortcut`.

### Task 5: Verificación integrada

**Files:**
- Modify: `docs/manual-tests/audio-smoke.md`
- Modify: `docs/superpowers/plans/2026-08-14-private-scene-tools.md`

**Interfaces:**
- Verifies: análisis, caché, A–B, favoritos y privacidad junto al rediseño IgoonTube.

- [ ] Ejecutar pruebas Release, análisis corto del video proporcionado y dos paneles.
- [ ] Verificar que el índice sea JSON pequeño y que no queden medios temporales.
- [ ] Probar atajo de privacidad, pantalla completa e instalador.
- [ ] Commit: `test: verify private scene tools`.
