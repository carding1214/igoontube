# IgoonTube NoAI Distribution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build full and lightweight NoAI variants without breaking local playback or non-AI tools.

**Architecture:** MSBuild property `NoAI=true` defines `IGOONTUBE_NO_AI`; a compile-time capability flag hides AI-only UI and prevents AI factory creation. A parameterized publisher produces either the existing full tree or a NoAI tree that never copies Python, models or the vision host. A separate Inno script gives NoAI its own AppId and filenames.

**Tech Stack:** .NET 8, WPF, MSBuild, PowerShell, Inno Setup, xUnit.

## Global Constraints

- Preserve the existing full build and filenames.
- Keep libmpv and FFmpeg in both variants.
- NoAI excludes Python, PyTorch, Demucs, MediaPipe, AI models and `vision_host.py`.
- NoAI hides AI audio, tracking and automatic scene analysis.
- Both variants remain unsigned, local-only and installable together.
- All build caches and temporary files stay on `F:`.

---

### Task 1: Compile-time capability

**Files:**
- Modify: `src/PUPlayer.App/PUPlayer.App.csproj`
- Create: `src/PUPlayer.App/Features/BuildCapabilities.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Test: `tests/PUPlayer.IntegrationTests/UI/NoAiBuildTests.cs`

**Interfaces:**
- Produces: `BuildCapabilities.AiAvailable`.
- Produces: `PlayerPanelViewModel.AiFeaturesAvailable`.

- [ ] Write a failing test that compiles against `BuildCapabilities` and expects normal builds to expose AI.

```csharp
[Fact]
public void DefaultBuild_ExposesAiFeatures() => Assert.True(BuildCapabilities.AiAvailable);
```
- [ ] Run the focused test and verify the missing-type failure.
- [ ] Add `<DefineConstants Condition="'$(NoAI)' == 'true'">$(DefineConstants);IGOONTUBE_NO_AI</DefineConstants>`.
- [ ] Implement `AiAvailable` with `#if IGOONTUBE_NO_AI` and expose it from each panel.
- [ ] In `CreateFeatures`, pass null factories for audio separation, vision and scenes when AI is unavailable; keep clips and thumbnails.
- [ ] Build both properties:

```powershell
powershell -File scripts\dotnet.ps1 build src\PUPlayer.App\PUPlayer.App.csproj -c Release --no-restore
powershell -File scripts\dotnet.ps1 build src\PUPlayer.App\PUPlayer.App.csproj -c Release --no-restore -p:NoAI=true
```

- [ ] Commit: `feat: add no-ai build capability`.

### Task 2: Hide unsupported controls

**Files:**
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Modify: `tests/PUPlayer.IntegrationTests/UI/NoAiBuildTests.cs`

**Interfaces:**
- Consumes: `AiFeaturesAvailable`.

- [ ] Write failing XAML-behavior tests that load the view model capability and verify AI groups collapse when false.

```csharp
[Fact]
public void Panel_ReportsCompiledAiCapability()
{
    var panel = new PlayerPanelViewModel(new FakePlayerBackend());
    Assert.Equal(BuildCapabilities.AiAvailable, panel.AiFeaturesAvailable);
}
```
- [ ] Wrap tracking button/status, scene sensitivity/analyze/cancel controls, and the complete AI-audio section in containers whose style collapses on `AiFeaturesAvailable=false`.
- [ ] Keep `Add favorite`, marker list and manual audio controls visible.
- [ ] Run focused UI tests for both build properties.
- [ ] Commit: `feat: hide ai-only controls in lightweight build`.

### Task 3: NoAI portable package

**Files:**
- Modify: `scripts/publish-portable.ps1`
- Create: `docs/IgoonTube-NoAI-README.txt`
- Modify: `tests/PUPlayer.IntegrationTests/App/DistributionTests.cs`

**Interfaces:**
- `publish-portable.ps1 -NoAI` produces `dist\IgoonTube-NoAI`.
- Default invocation still produces `dist\IgoonTube`.

- [ ] Write a failing distribution test that invokes a package-layout helper on a controlled staging tree and asserts NoAI excludes `.tools\ai`, `data\models` and `scripts\vision_host.py` while retaining `ffmpeg.exe` and `libmpv-2.dll`.

```powershell
powershell -File scripts\publish-portable.ps1 -NoAI
if (Test-Path F:\PUPlayer\dist\IgoonTube-NoAI\.tools\ai) { exit 1 }
if (Test-Path F:\PUPlayer\dist\IgoonTube-NoAI\data\models) { exit 1 }
if (Test-Path F:\PUPlayer\dist\IgoonTube-NoAI\scripts\vision_host.py) { exit 1 }
if (!(Test-Path F:\PUPlayer\dist\IgoonTube-NoAI\ffmpeg.exe)) { exit 1 }
if (!(Test-Path F:\PUPlayer\dist\IgoonTube-NoAI\libmpv-2.dll)) { exit 1 }
```
- [ ] Add `[switch]$NoAI`; choose target/readme and append `-p:NoAI=true` only for NoAI.
- [ ] Skip all Python/model/vision copies and AI runtime verification when `-NoAI`; retain worker self-test.
- [ ] Publish NoAI and assert forbidden paths do not exist.
- [ ] Commit: `build: add no-ai portable package`.

### Task 4: Independent NoAI installer

**Files:**
- Create: `installer/IgoonTube-NoAI.iss`
- Modify: `scripts/build-installer.ps1`
- Modify: `tests/PUPlayer.IntegrationTests/App/DistributionTests.cs`

**Interfaces:**
- `build-installer.ps1 -NoAI` consumes `dist\IgoonTube-NoAI`.
- Produces `IgoonTube-NoAI-Setup.exe` and matching `.bin` parts.

- [ ] Write a failing installer metadata test for distinct AppId, source directory, default install directory and output prefix.

```powershell
powershell -File scripts\build-installer.ps1 -NoAI
if (!(Test-Path F:\PUPlayer\dist\IgoonTube-NoAI-Setup.exe)) { exit 1 }
```
- [ ] Add the dedicated Inno definition, keeping optional associations and English/Spanish installer languages.
- [ ] Parameterize the build script to select the correct `.iss`.
- [ ] Compile NoAI installer and regenerate hashes.
- [ ] Commit: `build: add no-ai installer`.

### Task 5: Verification and release

**Files:**
- Modify: `docs/IgoonTube-LEEME.txt`
- Modify: `CONTINUE-PUPLAYER.md`

- [ ] Redirect NuGet HTTP cache to `F:\PUPlayer\.nuget-http-cache` during restore so C: is not used.
- [ ] Run the complete Release suite with the supplied real video.
- [ ] Publish and self-test both portable variants.
- [ ] Verify NoAI playback, seek, manual audio, thumbnail and clip export with the supplied video.
- [ ] Compile both installers.
- [ ] Recreate `IgoonTube-Source.zip`, regenerate and verify every SHA-256 line.
- [ ] Record final sizes and confirm no owned IgoonTube, worker, FFmpeg or Python process remains.
- [ ] Commit: `docs: document full and no-ai releases`.
