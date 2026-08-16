# Balanced Lazy Loading Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show IgoonTube immediately, reach the first playable frame quickly, and initialize every optional processor only when requested.

**Architecture:** A panel owns a small loading state machine and receives lazy feature factories instead of constructed FFmpeg/Python/vision services. mpv remains one worker per panel with bounded memory read-ahead. Cache scans and favorites move off the blocking startup path.

**Tech Stack:** .NET 8, WPF, CommunityToolkit.Mvvm, libmpv, xUnit.

## Global Constraints

- Local files only; never start network work.
- Real windows and both panels remain independent.
- Original audio remains the default.
- Optional failures never stop normal playback.
- No persistent timing logs or media paths in diagnostics.
- Preserve the unsigned portable and installer outputs on `F:`.

---

### Task 1: Loading state and deferred favorites

**Files:**
- Create: `src/PUPlayer.Core/Favorites/IFavoriteStore.cs`
- Modify: `src/PUPlayer.Core/Favorites/FavoriteStore.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Fakes/FakePlayerBackend.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`
- Modify: `src/PUPlayer.App/Localization/Strings.en.xaml`
- Modify: `src/PUPlayer.App/Localization/Strings.es.xaml`

**Interfaces:**
- Produces: `bool IsLoading`, `string LoadStatus`, `bool HasPlayableFrame`.
- Produces: constructor delay injection `Func<TimeSpan, CancellationToken, Task>? delay = null`.
- Produces: `IFavoriteStore.Load(string)` and `IFavoriteStore.Save(string, IEnumerable<double>)` for deferred, countable access.
- A first `PlayerSnapshot` with `DurationSeconds > 0` marks the panel playable.

- [ ] **Step 1: Write failing loading and favorites tests**

Add a channel-backed `FakePlayerBackend.Publish(PlayerSnapshot)` and tests equivalent to:

```csharp
[Fact]
public async Task Load_RemainsLoadingUntilFirstPlayableSnapshot()
{
    var backend = new FakePlayerBackend();
    var panel = new PlayerPanelViewModel(backend, "video.mp4");
    await panel.LoadAsync(1);
    Assert.True(panel.IsLoading);
    backend.Publish(new(0, 60, false, 1, 100));
    await panel.WaitForPlayableFrameAsync().WaitAsync(TimeSpan.FromSeconds(1));
    Assert.False(panel.IsLoading);
}

[Fact]
public void Constructor_DoesNotReadFavorites() { /* counting store remains at zero */ }
```

- [ ] **Step 2: Run RED**

Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~PlayerPanelViewModelTests"
```

Expected: compile failures for missing loading members and `Publish`.

- [ ] **Step 3: Implement the loading state machine**

Start `ObserveAsync` before `backend.LoadAsync`. Set `IsLoading=true` and localized `Loading…`; complete an internal `TaskCompletionSource` on the first playable snapshot. Start a cancellable ten-second delay that changes the label to `Still loading…`. On load failure set `IsLoading=false`, preserve the concise error, and leave the panel closable.

Expose `WaitForPlayableFrameAsync()` for deterministic tests and post-load coordination:

```csharp
public Task WaitForPlayableFrameAsync() => playable.Task;
```

- [ ] **Step 4: Move favorites after the first frame**

Remove `LoadFavorites()` from the constructor. After the first playable snapshot, run `favoriteStore.Load(MediaPath)` on the thread pool and marshal marker changes through the captured synchronization context. Guard it with `Interlocked.Exchange` so it runs once.

- [ ] **Step 5: Run GREEN and commit**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~PlayerPanelViewModelTests"
git add src/PUPlayer.App tests/PUPlayer.IntegrationTests
git commit -m "perf: defer panel metadata until first frame"
```

### Task 2: Non-modal loading overlay

**Files:**
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `tests/PUPlayer.IntegrationTests/UI/CarbonThemeTests.cs`

**Interfaces:**
- Consumes: `IsLoading`, `LoadStatus`, `HasPlayableFrame` from Task 1.

- [ ] **Step 1: Write failing XAML contract tests**

Assert the player XAML contains a named `LoadingOverlay`, binds visibility to `IsLoading`, binds text to `LoadStatus`, and keeps `IsHitTestVisible="False"`.

- [ ] **Step 2: Run RED**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~CarbonThemeTests"
```

- [ ] **Step 3: Add the compact overlay**

Place it after `MpvSurface` and before `CropCanvas`:

```xml
<Border x:Name="LoadingOverlay" IsHitTestVisible="False"
        HorizontalAlignment="Center" VerticalAlignment="Center"
        Background="#CC14161A" CornerRadius="7" Padding="12,7">
  <Border.Style><Style TargetType="Border"><Setter Property="Visibility" Value="Collapsed"/>
    <Style.Triggers><DataTrigger Binding="{Binding IsLoading}" Value="True"><Setter Property="Visibility" Value="Visible"/></DataTrigger></Style.Triggers>
  </Style></Border.Style>
  <TextBlock Text="{Binding LoadStatus}" Foreground="{DynamicResource TextBrush}"/>
</Border>
```

- [ ] **Step 4: Run GREEN and commit**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~CarbonThemeTests"
git add src/PUPlayer.App/Views tests/PUPlayer.IntegrationTests/UI
git commit -m "feat: show nonblocking video loading state"
```

### Task 3: Lazy optional feature factories

**Files:**
- Create: `src/PUPlayer.App/Features/PlayerFeatureFactories.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Workspace/WorkspaceViewModelTests.cs`

**Interfaces:**
- Produces:

```csharp
public sealed class PlayerFeatureFactories(
    Func<IAudioSeparationService?> audio,
    Func<IVisionDetector?> vision,
    Func<ISceneAnalysisService?> scenes,
    Func<IClipExportService?> clips,
    Func<IThumbnailService?> thumbnails)
```

- Each property uses `Lazy<T?>` with `LazyThreadSafetyMode.ExecutionAndPublication`.
- `WorkspaceViewModel` consumes `Func<PlayerFeatureFactories>? featureFactory` and creates one set per panel.

- [ ] **Step 1: Write failing factory-count tests**

Count every delegate call. Construct and load a panel; assert all counts are zero. Then call the matching action and assert only that factory becomes one. Cover tracking, voice enhancement, scene analysis, thumbnail retrieval and clip export.

- [ ] **Step 2: Run RED**

Run the two focused test classes; expect missing `PlayerFeatureFactories`.

- [ ] **Step 3: Implement the lazy container and change consumers**

Resolve features only inside `StartTrackingAsync`, `EnhanceAudioAsync`, `AnalyzeScenesAsync`, `GetThumbnailAsync` and `ExportClipAsync`. Create `VisionCoordinator` on first tracking use and dispose only created vision objects.

In `MainWindow`, replace eager calls to `CreateAudioService()` and `CreateVisionDetector()` with closures. The scene closure reuses that panel's lazy audio instance.

- [ ] **Step 4: Run GREEN and commit**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~PlayerPanelViewModelTests|FullyQualifiedName~WorkspaceViewModelTests"
git add src/PUPlayer.App tests/PUPlayer.IntegrationTests
git commit -m "perf: initialize optional processors on demand"
```

### Task 4: Remove startup cache scans

**Files:**
- Create: `src/PUPlayer.Core/Cache/ICacheCatalog.cs`
- Modify: `src/PUPlayer.Core/Cache/IgoonTubeCacheManager.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Workspace/WorkspaceViewModelTests.cs`

**Interfaces:**
- `Relocalize()` updates labels without calling `ScanGlobal()`.
- `RefreshGlobalCache()` remains the only normal UI path that performs a global scan.

- [ ] **Step 1: Add a counting cache-manager seam and failing test**

Extract `ICacheCatalog` with `ScanVideo`, `ScanGlobal`, `DeleteVideo`, and `DeleteGlobal`; implement it in `IgoonTubeCacheManager`. Construct/relocalize a workspace and assert `ScanGlobalCalls == 0`; call `RefreshGlobalCache()` and assert one.

- [ ] **Step 2: Run RED, implement, and run GREEN**

Initialize `GlobalCacheStatus` to localized `Cache` without size. Do not refresh it from the constructor or language/theme callback. Keep explicit popup refresh unchanged.

- [ ] **Step 3: Commit**

```powershell
git add src/PUPlayer.App src/PUPlayer.Core tests/PUPlayer.IntegrationTests
git commit -m "perf: scan cache only when requested"
```

### Task 5: Balanced mpv read-ahead and local timing

**Files:**
- Create: `src/PUPlayer.Core/Playback/PlayerLoadMetrics.cs`
- Create: `src/PUPlayer.MpvWorker/Interop/MpvPlaybackOptions.cs`
- Modify: `src/PUPlayer.App/App.xaml.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `src/PUPlayer.MpvWorker/Interop/MpvClient.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Worker/PlayerWorkerTests.cs`
- Modify: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces `PlayerLoadMetrics(Func<long>? timestamp = null)` with first-only `WindowVisible`, `WorkerReady` and `FirstPlayableFrame` marks; no path or persistent writer.
- Produces `MpvPlaybackOptions.Values`, the single option map consumed by `MpvClient` and tests.
- mpv options: `cache=yes`, `cache-secs=5`, `demuxer-readahead-secs=5`, `demuxer-max-bytes=64MiB`, `demuxer-max-back-bytes=16MiB`, `cache-pause-initial=no`.

- [ ] **Step 1: Write failing option and metric tests**

Assert `MpvPlaybackOptions.Values` contains the exact map. With a fake monotonic clock, assert `WindowVisible` is first-only, `WorkerReady` is marked after `backend.LoadAsync`, and `FirstPlayableFrame` follows the first playable snapshot.

- [ ] **Step 2: Run RED**

Run focused worker and panel tests; expect missing options/type.

- [ ] **Step 3: Implement bounded read-ahead and metrics**

Keep `hwdec=auto-safe`, `demuxer-thread=yes`, in-memory cache only, and no initial cache pause. Create one metrics object in `App`, mark visibility from `ContentRendered`, and pass it to the initial panel; first-only setters prevent later panels from overwriting startup timing. Never log it or include the media path.

- [ ] **Step 4: Run GREEN and commit**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test tests\PUPlayer.IntegrationTests\PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~PlayerWorkerTests|FullyQualifiedName~PlayerPanelViewModelTests"
git add src tests
git commit -m "perf: balance local playback read ahead"
```

### Task 6: Full verification and release refresh

**Files:**
- Modify: `docs/manual-tests/core-smoke.md`
- Modify: `CONTINUE-PUPLAYER.md`

**Interfaces:**
- Produces refreshed portable app, installer, source ZIP and `SHA256SUMS.txt` under `F:\PUPlayer\dist`.

- [ ] **Step 1: Add manual performance checks**

Document visible-window/first-frame timing, two simultaneous panels, absence of optional child processes at startup, and first-use activation of each feature.

- [ ] **Step 2: Run the complete suite with the real video**

```powershell
$env:IGOONTUBE_REAL_VIDEO='F:\jeje\grabaciones wsr 3.9.9.1\princesita_valija_\princesita_valija__Stripchat_20260516_200017-cut-merged-1779078827096.mp4'
powershell -ExecutionPolicy Bypass -File scripts\dotnet.ps1 test PUPlayer.sln -c Release --no-restore
```

Expected: all tests pass, including the real-media smoke test.

- [ ] **Step 3: Perform bounded UI/process verification**

Open the supplied video and record visible-window and first-frame times. Confirm no owned Python/FFmpeg process exists before using an optional feature. Open a second video, seek both, then activate thumbnails, tracking, AI, scenes and clip export one at a time.

- [ ] **Step 4: Rebuild and verify delivery**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-portable.ps1
powershell -ExecutionPolicy Bypass -File scripts\build-installer.ps1
git archive --format=zip --output=F:\PUPlayer\dist\IgoonTube-Source.zip HEAD
powershell -ExecutionPolicy Bypass -File scripts\write-release-hashes.ps1 -DistRoot F:\PUPlayer\dist
```

Verify every hash line against `Get-FileHash` and retain both installer parts.

- [ ] **Step 5: Commit documentation and clean generated development files**

Delete only owned test output and temporary build files; preserve `dist`, runtimes, models, caches and media.

```powershell
git add docs CONTINUE-PUPLAYER.md
git commit -m "docs: verify balanced lazy loading"
```
