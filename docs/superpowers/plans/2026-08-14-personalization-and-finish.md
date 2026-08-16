# IgoonTube Personalization and Finish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir configuración global, temas modificables, inglés/español, exportación segura, confirmaciones de caché y entrega verificable sin firma.

**Architecture:** La configuración determinista vive en `PUPlayer.Core`; WPF aplica recursos dinámicos y observa el JSON compartido entre procesos. Los selectores y diálogos permanecen en la vista, mientras los cálculos de espacio y resultados de caché son modelos probables sin UI.

**Tech Stack:** .NET 8, WPF, CommunityToolkit.Mvvm, System.Text.Json, xUnit, FFmpeg e Inno Setup.

## Global Constraints

- Inglés es el idioma inicial y Carbon Blue el tema inicial.
- Configuración, modelos y temporales propios permanecen en F:.
- Máximo dos videos por ventana; reproducción y controles siguen independientes.
- Ningún cambio reinicia reproducción, audio, zoom o seguimiento.
- El instalador permanece sin firma y se publican hashes SHA-256.
- Implementación directa, sin subagentes.

---

### Task 1: Modelo y almacenamiento global

**Files:**
- Create: `src/PUPlayer.Core/Settings/AppSettings.cs`
- Create: `src/PUPlayer.Core/Settings/AppSettingsStore.cs`
- Test: `tests/PUPlayer.Core.Tests/Settings/AppSettingsTests.cs`
- Test: `tests/PUPlayer.Core.Tests/Settings/AppSettingsStoreTests.cs`

**Interfaces:**
- Produces: `ThemePreset`, `ControlDensity`, `AppSettings.Default`, `AppSettings.ApplyPreset(ThemePreset)`, `AppSettings.Normalize()`.
- Produces: `AppSettingsStore(string path)`, `Load()`, `Save(AppSettings)`, `Changed`, `Dispose()`.

- [ ] **Step 1: Write failing settings tests**

```csharp
[Fact]
public void Defaults_AreEnglishCarbonBlue() =>
    Assert.Equal(new AppSettings(1, "en", ThemePreset.CarbonBlue, "#2F8CFF", 7, 32, ControlDensity.Compact, null), AppSettings.Default);

[Theory]
[InlineData(ThemePreset.MidnightSoft, "#60A5FA", 18, 34)]
[InlineData(ThemePreset.GraphiteCompact, "#1683E8", 3, 28)]
public void Preset_AppliesExactTokens(ThemePreset preset, string accent, int radius, int height)
{
    var value = AppSettings.Default.ApplyPreset(preset);
    Assert.Equal((accent, radius, height), (value.AccentColor, value.ButtonCornerRadius, value.ControlHeight));
}
```

- [ ] **Step 2: Run RED**

Run: `powershell -ExecutionPolicy Bypass -File scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests/PUPlayer.Core.Tests.csproj -c Release --filter "FullyQualifiedName~Settings"`

Expected: FAIL because `PUPlayer.Core.Settings` does not exist.

- [ ] **Step 3: Implement immutable settings and normalization**

Use records and enums; accept only `en`/`es`, `#RRGGBB`, radius `0..20`, height `28..40`, and an existing local export directory or `null`. Manual token changes set `ThemePreset.Custom` in the settings controller, not inside `Normalize()`.

- [ ] **Step 4: Add failing store tests**

```csharp
[Fact]
public void CorruptJson_ReturnsDefaults()
{
    File.WriteAllText(path, "{");
    using var store = new AppSettingsStore(path);
    Assert.Equal(AppSettings.Default, store.Load());
}

[Fact]
public void Save_IsReusableBySecondStore()
{
    using var first = new AppSettingsStore(path);
    using var second = new AppSettingsStore(path);
    first.Save(AppSettings.Default with { Language = "es" });
    Assert.Equal("es", second.Load().Language);
    Assert.False(File.Exists(path + ".tmp"));
}
```

- [ ] **Step 5: Run RED, implement atomic JSON and watcher, then run GREEN**

Use `Directory.CreateDirectory`, serialize to `settings.json.tmp`, `File.Move(..., true)`, and a debounced `FileSystemWatcher`. Suppress duplicate notifications by comparing normalized values.

- [ ] **Step 6: Commit**

```powershell
git add src/PUPlayer.Core/Settings tests/PUPlayer.Core.Tests/Settings
git commit -m "feat: persist global appearance settings"
```

### Task 2: Localización y tema dinámico

**Files:**
- Create: `src/PUPlayer.App/Localization/Strings.en.xaml`
- Create: `src/PUPlayer.App/Localization/Strings.es.xaml`
- Create: `src/PUPlayer.App/Personalization/LocalizationService.cs`
- Create: `src/PUPlayer.App/Personalization/ThemeService.cs`
- Modify: `src/PUPlayer.App/App.xaml`
- Modify: `src/PUPlayer.App/Themes/CarbonControls.xaml`
- Test: `tests/PUPlayer.IntegrationTests/UI/PersonalizationTests.cs`

**Interfaces:**
- Consumes: `AppSettings`.
- Produces: `LocalizationService.Apply(string language)`, `LocalizationService.Text(string key)`.
- Produces: `ThemeService.Apply(AppSettings settings)`.

- [ ] **Step 1: Write failing resource parity and theme tests**

```csharp
[Fact]
public void Languages_ExposeIdenticalKeys()
{
    var en = Load("Localization/Strings.en.xaml");
    var es = Load("Localization/Strings.es.xaml");
    Assert.Equal(en.Keys.Cast<object>().OrderBy(x => x), es.Keys.Cast<object>().OrderBy(x => x));
    Assert.Equal("Settings", en["Settings"]);
    Assert.Equal("Ajustes", es["Settings"]);
}

[Fact]
public void CarbonBlue_AppliesApprovedTokens()
{
    var resources = new ResourceDictionary();
    new ThemeService(resources).Apply(AppSettings.Default);
    Assert.Equal(Color.FromRgb(0x2F, 0x8C, 0xFF), ((SolidColorBrush)resources["AccentBrush"]).Color);
    Assert.Equal(new CornerRadius(7), resources["ButtonCornerRadius"]);
}
```

- [ ] **Step 2: Run RED**

Run: `powershell -ExecutionPolicy Bypass -File scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~Personalization"`

Expected: FAIL because resources and services do not exist.

- [ ] **Step 3: Implement dictionaries and services**

Move every visible literal from `WorkspaceView.xaml` and `PlayerPanelView.xaml` to stable keys. Replace fixed radii/heights in shared button styles with `DynamicResource ButtonCornerRadius` and `DynamicResource ControlHeight`. Update mutable brushes in place so existing controls repaint without recreation.

- [ ] **Step 4: Run GREEN and existing UI tests**

Run the focused filter and `--filter "FullyQualifiedName~UI"`; expected: all pass.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.App/Localization src/PUPlayer.App/Personalization src/PUPlayer.App/App.xaml src/PUPlayer.App/Themes tests/PUPlayer.IntegrationTests/UI
git commit -m "feat: apply dynamic themes and languages"
```

### Task 3: Panel de ajustes y sincronización entre ventanas

**Files:**
- Create: `src/PUPlayer.App/ViewModels/SettingsViewModel.cs`
- Modify: `src/PUPlayer.App/App.xaml.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml`
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Test: `tests/PUPlayer.IntegrationTests/UI/SettingsViewModelTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/UI/PersonalizationTests.cs`

**Interfaces:**
- Consumes: store, localization and theme services.
- Produces: `SettingsViewModel.SelectPreset`, `SetLanguage`, `SetAccent`, `SetRadius`, `SetHeight`, `SetDensity`, `RestorePreset`.

- [ ] **Step 1: Write failing view-model tests**

```csharp
[Fact]
public void ManualAccent_BecomesCustomAndPersists()
{
    var vm = Create();
    vm.SetAccent("#0055FF");
    Assert.Equal(ThemePreset.Custom, vm.Value.ThemePreset);
    Assert.Equal("#0055FF", store.Load().AccentColor);
}

[Fact]
public void LanguageChange_DoesNotTouchPlayerState()
{
    var panel = CreatePanel();
    var before = (panel.PositionSeconds, panel.VolumePercent, panel.ZoomScale);
    settings.SetLanguage("es");
    Assert.Equal(before, (panel.PositionSeconds, panel.VolumePercent, panel.ZoomScale));
}
```

- [ ] **Step 2: Run RED, implement minimal view model, run GREEN**

The setter pipeline is `normalize -> save -> apply language -> apply theme`; incoming watcher events run only `apply`.

- [ ] **Step 3: Add settings popup to the compact header**

Use one `Settings` toggle beside global cache. Add ComboBoxes for language/preset/density, native color text input with validation, sliders for radius/height, preview buttons, and restore action. Bind all labels via `DynamicResource`; preserve one-line player bars.

- [ ] **Step 4: Localize runtime statuses**

Replace hard-coded status assignments in both view models with `ITextProvider.Text(key)` plus formatted arguments. Keep FFmpeg stderr unchanged after a localized prefix.

- [ ] **Step 5: Verify two store instances propagate within one second**

Run focused Core and Integration settings tests; expected: pass without sleeps longer than the one-second requirement.

- [ ] **Step 6: Commit**

```powershell
git add src/PUPlayer.App tests/PUPlayer.IntegrationTests/UI
git commit -m "feat: add global personalization controls"
```

### Task 4: Destino, estimación y espacio de clips

**Files:**
- Create: `src/PUPlayer.Core/MediaTools/ClipSizeEstimator.cs`
- Create: `src/PUPlayer.App/MediaTools/IClipDestinationPicker.cs`
- Create: `src/PUPlayer.App/MediaTools/SaveFileClipDestinationPicker.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Test: `tests/PUPlayer.Core.Tests/MediaTools/ClipSizeEstimatorTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces: `ClipEstimateMode { Original, CurrentView }` and `ClipSizeEstimator.Estimate(sourceBytes, sourceDuration, selectionDuration, mode)`.
- Produces: `IClipDestinationPicker.Pick(defaultPath) -> string?`.

- [ ] **Step 1: Write failing estimator tests**

```csharp
[Fact]
public void Original_EstimatesProportionalBytesWithMargin() =>
    Assert.Equal(115_000_000, ClipSizeEstimator.Estimate(1_000_000_000, 1000, 100, ClipEstimateMode.Original));

[Fact]
public void CurrentView_UsesConfiguredBitrateWithMargin() =>
    Assert.Equal(175_260_000, ClipSizeEstimator.Estimate(1, 1, 100, ClipEstimateMode.CurrentView));
```

- [ ] **Step 2: Run RED, implement checked arithmetic and run GREEN**

Clamp invalid duration to a clear `ArgumentOutOfRangeException`; use `long` and ceiling.

- [ ] **Step 3: Write failing picker/space tests in the panel view model**

Cover cancel (no exporter call), selected destination, insufficient free bytes (no exporter call), and persistence of selected directory.

- [ ] **Step 4: Implement picker and free-space abstraction**

Use `Microsoft.Win32.SaveFileDialog` with `.mp4`, current incremental filename and valid saved directory. Inject `Func<string,long>` for available space in tests; production uses `new DriveInfo(Path.GetPathRoot(path)!).AvailableFreeSpace`.

- [ ] **Step 5: Run focused and full media-tool tests**

Expected: hybrid export tests remain green and no `.partial` remains on cancel/error.

- [ ] **Step 6: Commit**

```powershell
git add src/PUPlayer.Core/MediaTools src/PUPlayer.App/MediaTools src/PUPlayer.App/ViewModels src/PUPlayer.App/MainWindow.xaml.cs tests
git commit -m "feat: validate clip destinations and disk space"
```

### Task 5: Confirmaciones e informe de caché

**Files:**
- Modify: `src/PUPlayer.Core/Cache/CacheEntry.cs`
- Modify: `src/PUPlayer.Core/Cache/IgoonTubeCacheManager.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Modify: `src/PUPlayer.App/Views/WorkspaceView.xaml.cs`
- Test: `tests/PUPlayer.Core.Tests/Cache/IgoonTubeCacheManagerTests.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Produces: `CacheDeleteResult(long FreedBytes, int DeletedFiles, IReadOnlyList<string> FailedFiles)`.
- Changes: `DeleteVideo` and `DeleteGlobal` return `CacheDeleteResult`.

- [ ] **Step 1: Write failing partial-delete result tests**

Inject a file-delete delegate into `IgoonTubeCacheManager` for deterministic locked-file behavior; assert freed bytes, deleted count and failed path while favorites/unknown files remain.

- [ ] **Step 2: Run RED, implement result aggregation, run GREEN**

Catch only `IOException` and `UnauthorizedAccessException`; never count a failed deletion as freed.

- [ ] **Step 3: Add localized per-video confirmations**

Each button scans its category first, displays category and formatted size, calls delete only after `Yes`, then shows freed size plus failure count. Global handlers use the same localized formatter.

- [ ] **Step 4: Run cache and UI tests**

Expected: unknown files and favorites remain; confirmation text keys exist in both dictionaries.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.Core/Cache src/PUPlayer.App tests/PUPlayer.Core.Tests/Cache tests/PUPlayer.IntegrationTests
git commit -m "feat: confirm and report cache cleanup"
```

### Task 6: Documentación y entrega sin firma

**Files:**
- Create: `scripts/write-release-hashes.ps1`
- Modify: `scripts/build-installer.ps1`
- Modify: `scripts/publish-portable.ps1`
- Modify: `docs/IgoonTube-LEEME.txt`
- Modify: `PRODUCT.md`
- Modify: `CONTINUE-PUPLAYER.md`
- Modify: `docs/manual-tests/core-smoke.md`
- Test: `tests/PUPlayer.IntegrationTests/App/DistributionTests.cs`

**Interfaces:**
- Produces: `dist/SHA256SUMS.txt` with relative paths and lowercase SHA-256 hashes.

- [ ] **Step 1: Write failing distribution test**

Assert scripts contain no signing command, the hash script sorts files deterministically, documentation says English default/Spanish optional/unsigned, and stale roadmap claims are removed.

- [ ] **Step 2: Run RED, implement minimal hash generation, run GREEN**

Hash only final `.exe`, `.bin` and `.zip` files, excluding `SHA256SUMS.txt`; write UTF-8 without BOM.

- [ ] **Step 3: Run complete verification**

```powershell
powershell -ExecutionPolicy Bypass -File scripts/dotnet.ps1 test PUPlayer.sln -c Release
$env:IGOONTUBE_REAL_VIDEO='F:\jeje\grabaciones wsr 3.9.9.1\princesita_valija_\princesita_valija__Stripchat_20260516_200017-cut-merged-1779078827096.mp4'
powershell -ExecutionPolicy Bypass -File scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj -c Release
powershell -ExecutionPolicy Bypass -File scripts/publish-portable.ps1
powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1
```

- [ ] **Step 4: Manual bounded UI verification**

Open the supplied video, switch all three presets, customize radius/accent/height, switch EN/ES, open a second process, verify propagation, cancel/save a clip, reject/accept cache deletion, and confirm playback state remains intact.

- [ ] **Step 5: Clean generated development-only files**

Remove only `.playwright-cli`, `output/playwright`, the visual-companion session and build intermediates proven to be generated. Preserve `dist`, runtimes, models and user media.

- [ ] **Step 6: Commit final source and delivery metadata**

```powershell
git add scripts docs PRODUCT.md CONTINUE-PUPLAYER.md tests
git commit -m "build: finish unsigned personalized release"
```

- [ ] **Step 7: Rebuild source archive and verify hashes**

Create `dist/IgoonTube-Source.zip` from committed tracked source, regenerate `SHA256SUMS.txt`, verify every line, and confirm no IgoonTube, worker, Python or FFmpeg process remains.
