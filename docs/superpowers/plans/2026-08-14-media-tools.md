# IgoonTube Media Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Integrar clips rápidos, favoritos persistentes, transformaciones, miniaturas y limpieza segura de caché por video/global.

**Architecture:** La lógica determinista y de archivos vive en `PUPlayer.Core`; FFmpeg y WPF quedan en `PUPlayer.App`. Cada panel posee estado independiente. Los archivos generados usan manifiestos versionados dentro de `<video>.pucache`; nunca se modifica el original.

**Tech Stack:** .NET 8, C# 12, WPF, CommunityToolkit.Mvvm, xUnit, FFmpeg existente, mpv IPC existente.

**Global Constraints:** TDD; sin subagentes; solo archivos locales; máximo dos paneles; operaciones cancelables; salida `.partial` hasta completar; no borrar archivos no reconocidos; normalizar rutas; preservar audio original por defecto; interfaz carbón/azul/blanco y barra inferior de una línea.

---

## Task 1: Modelar clips, transformaciones y densidad de miniaturas

**Files:**
- Create: `src/PUPlayer.Core/MediaTools/ClipSelection.cs`
- Create: `src/PUPlayer.Core/MediaTools/VideoTransform.cs`
- Create: `src/PUPlayer.Core/MediaTools/ThumbnailPlan.cs`
- Create: `src/PUPlayer.Core/MediaTools/ClipOutputNamer.cs`
- Test: `tests/PUPlayer.Core.Tests/MediaTools/MediaToolModelsTests.cs`

**Step 1: Write failing tests**

```csharp
[Fact] public void Selection_NormalizesMarks() =>
    Assert.Equal(new ClipSelection(10, 20), ClipSelection.FromMarks(20, 10, 100));

[Theory]
[InlineData(120, 60)]
[InlineData(7200, 180)]
[InlineData(50000, 300)]
public void ThumbnailCount_Adapts(double duration, int expected) =>
    Assert.Equal(expected, ThumbnailPlan.ForDuration(duration).Count);

[Fact] public void OutputName_IncrementsWithoutOverwrite() { /* _clip_001, _clip_002 */ }

[Fact] public void Transform_FilterContainsSelectedOperations() {
    var value = new VideoTransform(90, true, false, new(.1, .2, .7, .6));
    Assert.Equal("crop=iw*0.7:ih*0.6:iw*0.1:ih*0.2,hflip,transpose=1", value.ToFfmpegFilter());
}
```

**Step 2: Run red**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj --filter MediaToolModelsTests`

Expected: FAIL, namespaces/types missing.

**Step 3: Implement minimal validated records**

```csharp
public readonly record struct ClipSelection(double Start, double End)
{
    public double Duration => End - Start;
    public static ClipSelection FromMarks(double a, double b, double mediaDuration) =>
        new(Math.Clamp(Math.Min(a, b), 0, mediaDuration), Math.Clamp(Math.Max(a, b), 0, mediaDuration));
}

public readonly record struct CropRect(double X, double Y, double Width, double Height);
public sealed record VideoTransform(int Rotation, bool MirrorX, bool MirrorY, CropRect? Crop);
```

Use `Count = clamp(round(duration / 40), 60, 300)`. Reject clips shorter than 0.25 s and crop dimensions below 0.02. `ClipOutputNamer` must use the source directory and never overwrite.

**Step 4: Run green and commit**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj --filter MediaToolModelsTests`

Commit: `git add src/PUPlayer.Core/MediaTools tests/PUPlayer.Core.Tests/MediaTools && git commit -m "feat: add media tool models"`

## Task 2: Persistir favoritos sin depender del análisis de escenas

**Files:**
- Create: `src/PUPlayer.Core/Favorites/FavoriteIndex.cs`
- Create: `src/PUPlayer.Core/Favorites/FavoriteStore.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Test: `tests/PUPlayer.Core.Tests/Favorites/FavoriteStoreTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Step 1: Write failing persistence tests**

```csharp
[Fact] public void RoundTrip_LoadsWithoutSceneAnalysis() {
    var store = new FavoriteStore(temp.Path);
    store.Save(video, [12.5, 44]);
    Assert.Equal([12.5, 44], store.Load(video).Seconds);
}

[Fact] public void ChangedSource_DoesNotReuseFavorites() { /* length/write-time identity */ }
```

Add an integration test that constructs a panel, calls `LoadFavorites()`, and sees favorite markers before `AnalyzeScenesAsync()`.

**Step 2: Run red**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj --filter FavoriteStoreTests`

**Step 3: Implement**

Store JSON at `<source>.pucache\favorites-v1.json` with source length, UTC write time, sorted unique seconds. `PlayerPanelViewModel` loads on construction, saves on add/remove, and merges favorites with analytical markers without duplicating positions within 0.1 s.

**Step 4: Verify and commit**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj --filter FavoriteStoreTests`

Run: `scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj --filter PlayerPanelViewModelTests`

Commit: `git add src tests && git commit -m "feat: persist independent favorites"`

## Task 3: Aplicar rotación, espejo y recorte en reproducción

**Files:**
- Modify: `src/PUPlayer.Core/Playback/PlayerRequest.cs`
- Modify: `src/PUPlayer.App/Playback/IPlayerBackend.cs`
- Modify: `src/PUPlayer.App/Playback/MpvWorkerBackend.cs`
- Modify: `src/PUPlayer.MpvWorker/IMpvClient.cs`
- Modify: `src/PUPlayer.MpvWorker/MpvClient.cs`
- Modify: `src/PUPlayer.MpvWorker/PlayerWorker.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Fakes/FakePlayerBackend.cs`
- Test: `tests/PUPlayer.Core.Tests/Playback/PlayerProtocolTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Step 1: Add failing protocol and isolation tests**

```csharp
[Fact] public void Geometry_Request_RoundTrips() {
    PlayerRequest value = new PlayerRequest.SetGeometry(7, new(90, true, false, null));
    Assert.IsType<PlayerRequest.SetGeometry>(Deserialize(Serialize(value)));
}

[Fact] public async Task Rotation_ChangesOnlyItsPanel() { /* left records geometry, right empty */ }
```

**Step 2: Run red**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj --filter PlayerProtocolTests`

**Step 3: Implement protocol and mpv mapping**

Add `SetGeometry`. In mpv map rotation to `video-rotate`, horizontal mirror to `video-mirror`, vertical mirror to `video-flip`; map crop to one labelled `vf` entry and remove only that label when reset. Keep current zoom/pan properties untouched so tracking and manual zoom continue working.

```csharp
await SetPropertyAsync("video-rotate", geometry.Rotation);
await SetPropertyAsync("video-mirror", geometry.MirrorX ? "yes" : "no");
await SetPropertyAsync("video-flip", geometry.MirrorY ? "yes" : "no");
await CommandAsync("vf", "remove", "@igoontube-crop");
if (geometry.Crop is { } c) await CommandAsync("vf", "add", $"@igoontube-crop:crop=iw*{c.Width}:ih*{c.Height}:iw*{c.X}:ih*{c.Y}");
```

**Step 4: Verify and commit**

Run both Core and Integration test projects.

Commit: `git add src tests && git commit -m "feat: add independent video geometry"`

## Task 4: Generar miniaturas progresivas con manifiesto

**Files:**
- Create: `src/PUPlayer.Core/Thumbnails/ThumbnailManifest.cs`
- Create: `src/PUPlayer.Core/Thumbnails/ThumbnailCache.cs`
- Create: `src/PUPlayer.App/MediaTools/IThumbnailService.cs`
- Create: `src/PUPlayer.App/MediaTools/FfmpegThumbnailService.cs`
- Test: `tests/PUPlayer.Core.Tests/Thumbnails/ThumbnailCacheTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/MediaTools/FfmpegThumbnailServiceTests.cs`

**Step 1: Write failing cache and command tests**

Test automatic count, source invalidation, nearest timestamp lookup, and FFmpeg arguments containing `-ss`, `-frames:v 1`, `scale=320:-2`, JPEG quality, and `.partial` rename.

**Step 2: Run red**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj --filter FfmpegThumbnailServiceTests`

**Step 3: Implement progressive generation**

Use `<source>.pucache\thumbnails-v1\manifest.json`. Generate the nearest hovered/scrubbed sample first, then neighbors by distance while idle. Limit to one FFmpeg process per panel and expose cancellation. Write JPEG at 320 px width and atomic rename from `.partial`.

**Step 4: Verify and commit**

Run matching tests, then commit: `feat: add progressive timeline thumbnails`.

## Task 5: Implementar exportación híbrida cancelable

**Files:**
- Create: `src/PUPlayer.App/MediaTools/IClipExportService.cs`
- Create: `src/PUPlayer.App/MediaTools/FfmpegProcessRunner.cs`
- Create: `src/PUPlayer.App/MediaTools/FfmpegKeyframeScanner.cs`
- Create: `src/PUPlayer.App/MediaTools/FfmpegClipExportService.cs`
- Test: `tests/PUPlayer.IntegrationTests/MediaTools/FfmpegKeyframeScannerTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/MediaTools/FfmpegClipExportServiceTests.cs`

**Step 1: Write failing strategy tests**

```csharp
[Fact] public async Task OriginalMode_UsesHybridAroundKeyframes() { /* edge encode + middle copy + concat */ }
[Fact] public async Task TransformedMode_ReencodesOnlySelection() { /* -ss/-t before filters */ }
[Fact] public async Task UnsupportedInput_FallsBackToSelectionOnlyEncode() { }
[Fact] public async Task Cancel_DeletesPartialFiles() { }
```

**Step 2: Run red**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj --filter "FfmpegClipExportServiceTests|FfmpegKeyframeScannerTests"`

**Step 3: Implement keyframe scan without FFprobe**

Run bundled FFmpeg with `-skip_frame nokey -i <source> -vf showinfo -an -f null NUL`; parse `pts_time`. Cache keyframes in memory per source identity.

**Step 4: Implement two export paths**

- Original view: for H.264/H.265 + AAC/Opus, encode only first/last GOP, stream-copy complete middle GOPs, concatenate compatible temporary segments, then validate duration. If compatibility or duration validation fails, delete intermediates and encode only `[A,B]`.
- Apply current view: encode only `[A,B]` using `VideoTransform.ToFfmpegFilter()`, H.264 CRF 18/preset fast, AAC 192k; preserve dimensions divisible by 2.
- Report phase/progress, estimate free space, use source folder, `_clip_NNN.mp4`, and rename `.partial` only on success.

**Step 5: Verify and commit**

Run integration filters. Commit: `feat: export clips with hybrid fast path`.

## Task 6: Administrar caché por video y globalmente

**Files:**
- Create: `src/PUPlayer.Core/Cache/CacheEntry.cs`
- Create: `src/PUPlayer.Core/Cache/IgoonTubeCacheManager.cs`
- Test: `tests/PUPlayer.Core.Tests/Cache/IgoonTubeCacheManagerTests.cs`

**Step 1: Write safety-first failing tests**

```csharp
[Fact] public void Scan_ReportsOnlyOwnedFiles() { /* known manifest + unknown file */ }
[Fact] public void DeleteVideoCache_PreservesFavoritesAndUnknownFiles() { }
[Fact] public void DeleteThumbnails_LeavesAudioCaches() { }
[Fact] public void DeleteGlobal_StaysInsideConfiguredRoots() { }
```

**Step 2: Run red**

Run: `scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj --filter IgoonTubeCacheManagerTests`

**Step 3: Implement allow-listed deletion**

Recognize only audio manifests/files already emitted by `AudioCacheLocator`, scene-analysis cache, `thumbnails-v1`, keyframe manifests, and `.partial` files carrying an IgoonTube prefix. Favorites are user data, not cache. Resolve every path and require it to remain inside either `<source>.pucache` or configured `F:\PUPlayer\data\cache`; return freed bytes and failures.

**Step 4: Verify and commit**

Run Core tests. Commit: `feat: add safe cache management`.

## Task 7: Integrar Herramientas en cada panel

**Files:**
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Create: `src/PUPlayer.App/Views/CropOverlay.xaml`
- Create: `src/PUPlayer.App/Views/CropOverlay.xaml.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Step 1: Add failing view-model tests**

Cover marks A/B, invalid range, unique output name, export mode, progress/cancel, 90° rotation cycle, independent mirrors/crop, favorite removal, per-video cache sizes/deletion, and thumbnail request for current cursor time.

**Step 2: Run red**

Run PlayerPanelViewModel tests.

**Step 3: Implement panel state**

Inject clip, thumbnail, favorites and cache services. Expose `ClipStart`, `ClipEnd`, `ClipMode`, `Geometry`, `ExportProgress`, `IsExporting`, `ThumbnailPath`, cache sizes, and async actions. Cancel background work on panel disposal.

**Step 4: Build the compact UI**

Replace the separate `Escenas` action with one compact `Herramientas` toggle. Its popup has four collapsible sections:

- `Clip`: A/B, duration, Original/Aplicar vista, Exportar, progress/cancel.
- `Imagen`: rotate left/right, mirror X/Y, `Recorte personalizado`, reset.
- `Favoritos`: one-click add and compact seek/delete list.
- `Caché`: audio/miniatures/analysis size and selective delete.

Add a timeline hover preview above the cursor and an interactive normalized crop overlay with drag/resize handles. Keep the main control strip single-line and horizontally scrollable. Use existing carbon tokens, blue active state, 36–42 px hit targets, consistent 6/10/14 px spacing, tooltips and automation names.

**Step 5: Verify UI build and commit**

Run Integration tests and `scripts\dotnet.ps1 build PUPlayer.sln -c Release`.

Commit: `git add src tests && git commit -m "feat: integrate per-video media tools"`

## Task 8: Añadir gestor global y empaquetado

**Files:**
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `scripts/publish-portable.ps1`
- Modify: `docs/IgoonTube-LEEME.txt`
- Test: `tests/PUPlayer.IntegrationTests/Workspace/WorkspaceViewModelTests.cs`

**Step 1: Add failing workspace tests**

Test that global scan aggregates known caches, deletion requires explicit category/all action, refreshes size, and does not affect open panel playback state.

**Step 2: Implement global popup**

Add a compact header button `Caché` showing total size. Popup lists Audio, Miniaturas, Análisis and Temporales with refresh/delete actions and confirmation for `Eliminar todo`. Do not include favorites.

**Step 3: Update docs/package**

Document exports, cache location, favorite persistence, transformation behavior and fallback. Keep only existing `ffmpeg.exe`; no FFprobe dependency.

**Step 4: Verify and commit**

Run all tests and Release build. Commit: `feat: add global cache tools`.

## Task 9: Validación real, limpieza y entregables

**Files:**
- Modify only if validation exposes defects.

**Step 1: Automated verification**

Run:

```powershell
scripts\dotnet.ps1 test tests\PUPlayer.Core.Tests\PUPlayer.Core.Tests.csproj -c Release
scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release
scripts\dotnet.ps1 build PUPlayer.sln -c Release
```

**Step 2: Real media smoke test**

Use `F:\jeje\grabaciones wsr 3.9.9.1\princesita_valija_\princesita_valija__Stripchat_20260516_200017-cut-merged-1779078827096.mp4` to verify:

- two simultaneous independent panels;
- seek/audio/zoom/geometry independence;
- favorite survives restart;
- thumbnail preview at beginning/middle/end;
- 20 s original-view clip and transformed clip;
- hybrid result starts/ends within 0.15 s and plays with audio;
- cancellation leaves no `.partial`;
- per-video/global cache deletion preserves source, clips, favorites and unknown files.

**Step 3: Publish and installer test**

Run `scripts\publish-portable.ps1`, `scripts\build-installer.ps1`, portable self-test, silent install/uninstall, then verify two installed processes can open different videos.

**Step 4: Safe cleanup**

Remove only validated build staging, test clips and owned test caches. Confirm no `.pyc`, stale `.partial`, old `PUPlayer*` deliverables or residual player/worker/FFmpeg processes. Never delete the supplied source video.

**Step 5: Final source archive and commit**

Create `F:\PUPlayer\dist\IgoonTube-Source.zip` from tracked source excluding `.git`, `.worktrees`, `bin`, `obj`, `.tools` and generated caches. Confirm Git clean and report exact artifact paths/sizes plus test counts.

Commit any validation fixes separately with a specific message.

## Plan self-review

- Covers every approved design requirement: hybrid/selectable export, favorites, rotation/mirrors/crop, automatic thumbnails, per-video/global cache deletion, compact panel UI, cancellation and safe paths.
- Uses no FFprobe or new large runtime.
- Keeps Core free of WPF/FFmpeg process dependencies.
- All mutable operations have isolation, invalidation and cleanup tests.
- Every implementation path and required type is named explicitly.
