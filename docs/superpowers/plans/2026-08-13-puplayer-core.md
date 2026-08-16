# PUPlayer Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir una primera versión ejecutable que abra videos locales en procesos independientes y permita dos reproductores por ventana con controles, audio y zoom manual separados.

**Architecture:** `PUPlayer.App` es una aplicación WPF sin instancia única. Cada panel crea un `PUPlayer.MpvWorker` que aloja libmpv en un proceso separado, renderiza dentro de un HWND proporcionado por WPF y se controla mediante JSON delimitado por saltos de línea sobre una tubería con nombre.

**Tech Stack:** .NET 8, WPF, libmpv, Win32 `HwndHost`, named pipes, `System.Text.Json`, CommunityToolkit.Mvvm 8.4.2, xUnit 2.9.3.

## Global Constraints

- Windows 10/11 x64.
- Solo rutas locales absolutas; rechazar URLs y archivos inexistentes.
- Máximo dos paneles, independientes y sin sincronización maestra.
- Volumen por panel de 0 a 200%; ambos audios pueden sonar simultáneamente.
- Zoom por panel desde ajuste completo hasta 8×.
- Sin configuración de mpv del usuario, historial, telemetría ni red.
- SDK, binarios, compilación y temporales bajo `F:\PUPlayer`.
- No implementar separación de audio ni seguimiento visual en esta fase.

---

## Estructura de archivos

```text
F:\PUPlayer
├─ global.json
├─ Directory.Build.props
├─ PUPlayer.sln
├─ scripts/
│  ├─ bootstrap.ps1
│  ├─ dotnet.ps1
│  └─ verify-bootstrap.ps1
├─ vendor/mpv/                 # ignorado por Git
├─ src/
│  ├─ PUPlayer.Core/
│  │  ├─ Playback/
│  │  ├─ Workspace/
│  │  └─ Zoom/
│  ├─ PUPlayer.MpvWorker/
│  │  ├─ Interop/
│  │  └─ Worker/
│  └─ PUPlayer.App/
│     ├─ Playback/
│     ├─ Views/
│     └─ ViewModels/
└─ tests/
   ├─ PUPlayer.Core.Tests/
   └─ PUPlayer.IntegrationTests/
```

### Task 1: Toolchain and solution shell

**Files:**
- Create: `scripts/bootstrap.ps1`
- Create: `scripts/dotnet.ps1`
- Create: `scripts/verify-bootstrap.ps1`
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `PUPlayer.sln`
- Create: `src/PUPlayer.Core/PUPlayer.Core.csproj`
- Create: `src/PUPlayer.MpvWorker/PUPlayer.MpvWorker.csproj`
- Create: `src/PUPlayer.App/PUPlayer.App.csproj`
- Create: `tests/PUPlayer.Core.Tests/PUPlayer.Core.Tests.csproj`
- Create: `tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: PowerShell 5+, Git and internet access during development only.
- Produces: `scripts/dotnet.ps1 <dotnet args>` and `vendor/mpv/libmpv-2.dll`.

- [ ] **Step 1: Write the failing bootstrap verification**

```powershell
$root = Split-Path $PSScriptRoot -Parent
$required = @(
  "$root\.tools\dotnet\dotnet.exe",
  "$root\vendor\mpv\libmpv-2.dll"
)
$missing = $required | Where-Object { -not (Test-Path -LiteralPath $_) }
if ($missing) { throw "Missing dependencies: $($missing -join ', ')" }
& "$root\.tools\dotnet\dotnet.exe" --version
```

- [ ] **Step 2: Run verification and confirm it fails**

Run: `powershell -ExecutionPolicy Bypass -File scripts/verify-bootstrap.ps1`

Expected: FAIL listing the local SDK and libmpv DLL.

- [ ] **Step 3: Implement local bootstrap and solution creation**

`scripts/bootstrap.ps1` must download only into F, resolve the latest retained x64 libmpv build recommended by mpv.io, record URLs and SHA-256 values, then create the solution:

```powershell
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$tools = "$root\.tools"
$dotnet = "$tools\dotnet"
$mpv = "$root\vendor\mpv"
$env:TEMP = "$root\work\temp"
$env:TMP = $env:TEMP
$env:DOTNET_CLI_HOME = "$root\.dotnet-home"
$env:NUGET_PACKAGES = "$root\.packages"
New-Item -ItemType Directory -Force $tools,$dotnet,$mpv,$env:TEMP,$env:DOTNET_CLI_HOME,$env:NUGET_PACKAGES | Out-Null

$installer = "$tools\dotnet-install.ps1"
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $installer
& $installer -Channel 8.0 -Quality GA -InstallDir $dotnet

$headers = @{ 'User-Agent' = 'PUPlayer-bootstrap' }
$release = Invoke-RestMethod https://api.github.com/repos/shinchiro/mpv-winbuild-cmake/releases/latest -Headers $headers
$asset = $release.assets | Where-Object name -Match '^mpv-dev-x86_64-[0-9]{8}-git-[^.]+\.7z$' | Select-Object -First 1
if (-not $asset) { throw 'Compatible libmpv release asset not found' }
$archive = "$tools\$($asset.name)"
Invoke-WebRequest $asset.browser_download_url -Headers $headers -OutFile $archive
$sevenZip = "$tools\7zr.exe"
Invoke-WebRequest https://www.7-zip.org/a/7zr.exe -OutFile $sevenZip
& $sevenZip x $archive "-o$mpv" -y | Out-Null
$dll = Get-ChildItem $mpv -Recurse -Filter 'libmpv-2.dll' | Select-Object -First 1
if (-not $dll) { throw 'libmpv-2.dll missing after extraction' }
$targetDll = "$mpv\libmpv-2.dll"
if ($dll.FullName -ne $targetDll) { Copy-Item $dll.FullName $targetDll -Force }
@{
  mpv = @{ url = $asset.browser_download_url; sha256 = (Get-FileHash $archive).Hash }
  dotnetChannel = '8.0'
} | ConvertTo-Json -Depth 3 | Set-Content "$tools\dependencies.lock.json" -Encoding UTF8
```

Use the local SDK to run these exact commands:

```powershell
scripts/dotnet.ps1 new sln -n PUPlayer
scripts/dotnet.ps1 new classlib -n PUPlayer.Core -o src/PUPlayer.Core -f net8.0
scripts/dotnet.ps1 new console -n PUPlayer.MpvWorker -o src/PUPlayer.MpvWorker -f net8.0
scripts/dotnet.ps1 new wpf -n PUPlayer.App -o src/PUPlayer.App -f net8.0
scripts/dotnet.ps1 new xunit -n PUPlayer.Core.Tests -o tests/PUPlayer.Core.Tests -f net8.0
scripts/dotnet.ps1 new xunit -n PUPlayer.IntegrationTests -o tests/PUPlayer.IntegrationTests -f net8.0
scripts/dotnet.ps1 sln add src/PUPlayer.Core src/PUPlayer.MpvWorker src/PUPlayer.App tests/PUPlayer.Core.Tests tests/PUPlayer.IntegrationTests
scripts/dotnet.ps1 add src/PUPlayer.MpvWorker reference src/PUPlayer.Core
scripts/dotnet.ps1 add src/PUPlayer.App reference src/PUPlayer.Core
scripts/dotnet.ps1 add tests/PUPlayer.Core.Tests reference src/PUPlayer.Core
scripts/dotnet.ps1 add tests/PUPlayer.IntegrationTests reference src/PUPlayer.Core src/PUPlayer.MpvWorker src/PUPlayer.App
scripts/dotnet.ps1 add src/PUPlayer.App package CommunityToolkit.Mvvm --version 8.4.2
```

Create `scripts/dotnet.ps1` so every command uses the SDK on F:

```powershell
$root = Split-Path $PSScriptRoot -Parent
$env:DOTNET_ROOT = "$root\.tools\dotnet"
$env:PUPLAYER_TEST_ROOT = "$root\work\tests"
$env:TEMP = "$root\work\temp"
$env:TMP = $env:TEMP
$env:DOTNET_CLI_HOME = "$root\.dotnet-home"
$env:NUGET_PACKAGES = "$root\.packages"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
New-Item -ItemType Directory -Force $env:PUPLAYER_TEST_ROOT,$env:TEMP,$env:DOTNET_CLI_HOME,$env:NUGET_PACKAGES | Out-Null
& "$env:DOTNET_ROOT\dotnet.exe" @args
exit $LASTEXITCODE
```

Use these exact repository settings:

```json
// global.json
{"sdk":{"version":"8.0.100","rollForward":"latestFeature","allowPrerelease":false}}
```

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

Add `.tools/`, `.packages/`, `.dotnet-home/`, `vendor/`, `work/`, `data/`, `**/bin/` and `**/obj/` to `.gitignore`. Pin test packages to `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 18.8.1 and `coverlet.collector` 10.0.1.
Set `tests/PUPlayer.IntegrationTests/PUPlayer.IntegrationTests.csproj` to `<TargetFramework>net8.0-windows</TargetFramework>` and `<UseWPF>true</UseWPF>` so it can reference the application project.

- [ ] **Step 4: Verify bootstrap and build pass**

Run: `powershell -ExecutionPolicy Bypass -File scripts/bootstrap.ps1`

Run: `powershell -ExecutionPolicy Bypass -File scripts/verify-bootstrap.ps1`

Run: `scripts/dotnet.ps1 build PUPlayer.sln -c Debug`

Expected: all commands exit 0; dependencies exist only under F.

- [ ] **Step 5: Commit**

```powershell
git add .gitignore global.json Directory.Build.props PUPlayer.sln scripts src tests
git commit -m "build: bootstrap local PUPlayer toolchain"
```

### Task 2: Workspace domain with two independent slots

**Files:**
- Create: `src/PUPlayer.Core/Workspace/LayoutMode.cs`
- Create: `src/PUPlayer.Core/Workspace/MediaSlot.cs`
- Create: `src/PUPlayer.Core/Workspace/WorkspaceState.cs`
- Test: `tests/PUPlayer.Core.Tests/Workspace/WorkspaceStateTests.cs`

**Interfaces:**
- Consumes: absolute local file paths.
- Produces: `WorkspaceState.Add`, `Remove`, `Replace`, `ToggleLayout` and immutable `Slots`.

- [ ] **Step 1: Write failing workspace tests**

```csharp
[Fact]
public void Add_AllowsTwoSlotsAndRejectsThird()
{
    var state = WorkspaceState.Empty.Add(@"F:\media\one.mp4").Add(@"F:\media\two.mp4");
    Assert.Equal(2, state.Slots.Count);
    Assert.Throws<InvalidOperationException>(() => state.Add(@"F:\media\three.mp4"));
}

[Fact]
public void ToggleLayout_SwitchesOnlySplitOrientations()
{
    var state = WorkspaceState.Empty.Add("a").Add("b");
    Assert.Equal(LayoutMode.SplitVertical, state.ToggleLayout().Layout);
    Assert.Equal(LayoutMode.SplitHorizontal, state.ToggleLayout().ToggleLayout().Layout);
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter WorkspaceStateTests`

Expected: FAIL because workspace types do not exist.

- [ ] **Step 3: Implement the immutable workspace model**

```csharp
public enum LayoutMode { Single, SplitHorizontal, SplitVertical }

public sealed record MediaSlot(Guid Id, string Path)
{
    public static MediaSlot Create(string path) => new(Guid.NewGuid(), path);
}

public sealed record WorkspaceState(IReadOnlyList<MediaSlot> Slots, LayoutMode Layout)
{
    public static WorkspaceState Empty { get; } = new([], LayoutMode.Single);
    public WorkspaceState Add(string path) => Slots.Count >= 2
        ? throw new InvalidOperationException("A mosaic supports two videos.")
        : Normalize(Slots.Append(MediaSlot.Create(path)).ToArray(), Layout);
    public WorkspaceState Remove(Guid id) => Normalize(Slots.Where(x => x.Id != id).ToArray(), Layout);
    public WorkspaceState Replace(Guid id, string path) => Normalize(Slots.Select(x => x.Id == id ? MediaSlot.Create(path) : x).ToArray(), Layout);
    public WorkspaceState ToggleLayout() => Slots.Count < 2 ? this : this with
    {
        Layout = Layout == LayoutMode.SplitHorizontal ? LayoutMode.SplitVertical : LayoutMode.SplitHorizontal
    };
    static WorkspaceState Normalize(IReadOnlyList<MediaSlot> slots, LayoutMode layout) =>
        new(slots, slots.Count < 2 ? LayoutMode.Single : layout == LayoutMode.Single ? LayoutMode.SplitHorizontal : layout);
}
```

- [ ] **Step 4: Run focused and full tests**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter WorkspaceStateTests`

Run: `scripts/dotnet.ps1 test PUPlayer.sln`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.Core/Workspace tests/PUPlayer.Core.Tests/Workspace
git commit -m "feat: model two-panel workspaces"
```

### Task 3: Per-panel zoom state and mpv transform mapping

**Files:**
- Create: `src/PUPlayer.Core/Zoom/NormalizedPoint.cs`
- Create: `src/PUPlayer.Core/Zoom/ZoomState.cs`
- Create: `src/PUPlayer.Core/Zoom/MpvTransform.cs`
- Test: `tests/PUPlayer.Core.Tests/Zoom/ZoomStateTests.cs`

**Interfaces:**
- Consumes: cursor coordinates normalized to 0..1 and drag deltas normalized to panel size.
- Produces: `ZoomState` and `MpvTransform(VideoZoom, VideoPanX, VideoPanY)`.

- [ ] **Step 1: Write failing zoom tests**

```csharp
[Fact]
public void ZoomAt_KeepsCursorSourcePointStable()
{
    var cursor = new NormalizedPoint(.75, .5);
    var next = ZoomState.Default.ZoomAt(2, cursor);
    Assert.Equal(2, next.Scale);
    Assert.Equal(.625, next.CenterX, 3);
}

[Fact]
public void ZoomAndPan_AreClamped()
{
    var state = ZoomState.Default.ZoomAt(99, new(1, 1)).PanBy(5, 5);
    Assert.Equal(8, state.Scale);
    Assert.InRange(state.CenterX, .0625, .9375);
    Assert.InRange(state.CenterY, .0625, .9375);
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter ZoomStateTests`

Expected: FAIL because zoom types do not exist.

- [ ] **Step 3: Implement cursor-anchored zoom and mapping**

```csharp
public readonly record struct NormalizedPoint(double X, double Y);

public sealed record ZoomState(double Scale, double CenterX, double CenterY)
{
    public static ZoomState Default { get; } = new(1, .5, .5);
    public ZoomState ZoomAt(double factor, NormalizedPoint cursor)
    {
        var scale = Math.Clamp(Scale * factor, 1, 8);
        var x = CenterX + (cursor.X - .5) / Scale - (cursor.X - .5) / scale;
        var y = CenterY + (cursor.Y - .5) / Scale - (cursor.Y - .5) / scale;
        return Clamp(scale, x, y);
    }
    public ZoomState PanBy(double dx, double dy) => Clamp(Scale, CenterX - dx / Scale, CenterY - dy / Scale);
    public MpvTransform ToMpv() => new(Math.Log2(Scale), .5 - CenterX, .5 - CenterY);
    static ZoomState Clamp(double scale, double x, double y)
    {
        var edge = .5 / scale;
        return new(scale, Math.Clamp(x, edge, 1 - edge), Math.Clamp(y, edge, 1 - edge));
    }
}

public sealed record MpvTransform(double VideoZoom, double VideoPanX, double VideoPanY);
```

- [ ] **Step 4: Run focused and full tests**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter ZoomStateTests`

Run: `scripts/dotnet.ps1 test PUPlayer.sln`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.Core/Zoom tests/PUPlayer.Core.Tests/Zoom
git commit -m "feat: add independent video transforms"
```

### Task 4: Typed app-to-worker protocol

**Files:**
- Create: `src/PUPlayer.Core/Playback/PlayerSnapshot.cs`
- Create: `src/PUPlayer.Core/Playback/LocalMediaPath.cs`
- Create: `src/PUPlayer.Core/Playback/PlayerRequest.cs`
- Create: `src/PUPlayer.Core/Playback/PlayerEvent.cs`
- Create: `src/PUPlayer.Core/Playback/PlayerProtocol.cs`
- Test: `tests/PUPlayer.Core.Tests/Playback/PlayerProtocolTests.cs`
- Test: `tests/PUPlayer.Core.Tests/Playback/LocalMediaPathTests.cs`

**Interfaces:**
- Consumes: typed records serialized one JSON object per line.
- Produces: request kinds `load`, `pause`, `seek`, `volume`, `speed`, `transform`, `shutdown`; event kinds `ready`, `snapshot`, `ended`, `error`.

- [ ] **Step 1: Write failing round-trip and validation tests**

```csharp
[Fact]
public void Request_RoundTripsWithoutTypeLoss()
{
    PlayerRequest request = new PlayerRequest.SetTransform(7, new(1, -.25, .1));
    var json = PlayerProtocol.Serialize(request);
    Assert.Equal(request, PlayerProtocol.DeserializeRequest(json));
}

[Theory]
[InlineData("https://example.com/video.mp4")]
[InlineData("file:///F:/video.mp4")]
public void Load_RejectsUrls(string value) =>
    Assert.Throws<ArgumentException>(() => PlayerRequest.Load.Create(1, value));

[Fact]
public void LocalMediaPath_AcceptsOnlyExistingAbsoluteFiles()
{
    var root = Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!;
    Directory.CreateDirectory(root);
    var file = Path.Combine(root, $"{Guid.NewGuid():N}.tmp");
    File.WriteAllBytes(file, []);
    try { Assert.True(LocalMediaPath.TryCreate(file, out _)); }
    finally { File.Delete(file); }
    Assert.False(LocalMediaPath.TryCreate("missing.mp4", out _));
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter PlayerProtocolTests`

Expected: FAIL because protocol types do not exist.

- [ ] **Step 3: Implement discriminated request/event records**

Use `JsonPolymorphic` and explicit derived type names:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Load), "load")]
[JsonDerivedType(typeof(SetPause), "pause")]
[JsonDerivedType(typeof(Seek), "seek")]
[JsonDerivedType(typeof(SetVolume), "volume")]
[JsonDerivedType(typeof(SetSpeed), "speed")]
[JsonDerivedType(typeof(SetTransform), "transform")]
[JsonDerivedType(typeof(Shutdown), "shutdown")]
public abstract record PlayerRequest(long Id)
{
    public sealed record Load(long Id, string Path) : PlayerRequest(Id)
    {
        public static Load Create(long id, string path)
        {
            if (!LocalMediaPath.TryCreate(path, out var local))
                throw new ArgumentException("Only absolute local paths are allowed.", nameof(path));
            return new(id, local.Value);
        }
    }
    public sealed record SetPause(long Id, bool Value) : PlayerRequest(Id);
    public sealed record Seek(long Id, double Seconds) : PlayerRequest(Id);
    public sealed record SetVolume(long Id, double Percent) : PlayerRequest(Id);
    public sealed record SetSpeed(long Id, double Value) : PlayerRequest(Id);
    public sealed record SetTransform(long Id, MpvTransform Value) : PlayerRequest(Id);
    public sealed record Shutdown(long Id) : PlayerRequest(Id);
}

public readonly record struct LocalMediaPath(string Value)
{
    public static bool TryCreate(string value, out LocalMediaPath path)
    {
        path = default;
        if (!Path.IsPathFullyQualified(value) || !File.Exists(value)) return false;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !uri.IsFile) return false;
        path = new(Path.GetFullPath(value));
        return true;
    }
}
```

`PlayerProtocol` uses one shared `JsonSerializerOptions` and rejects unknown types.

```csharp
public sealed record PlayerSnapshot(
    double PositionSeconds,
    double DurationSeconds,
    bool Paused,
    double Speed,
    double VolumePercent);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Ready), "ready")]
[JsonDerivedType(typeof(SnapshotChanged), "snapshot")]
[JsonDerivedType(typeof(Ended), "ended")]
[JsonDerivedType(typeof(Failed), "error")]
public abstract record PlayerEvent
{
    public sealed record Ready : PlayerEvent;
    public sealed record SnapshotChanged(PlayerSnapshot Value) : PlayerEvent;
    public sealed record Ended : PlayerEvent;
    public sealed record Failed(string Code, string Message) : PlayerEvent;
}

public static class PlayerProtocol
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);
    public static PlayerRequest DeserializeRequest(string value) =>
        JsonSerializer.Deserialize<PlayerRequest>(value, Json)
        ?? throw new JsonException("Request is empty.");
    public static PlayerEvent DeserializeEvent(string value) =>
        JsonSerializer.Deserialize<PlayerEvent>(value, Json)
        ?? throw new JsonException("Event is empty.");
}
```

- [ ] **Step 4: Run protocol and full tests**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.Core.Tests --filter PlayerProtocolTests`

Run: `scripts/dotnet.ps1 test PUPlayer.sln`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.Core/Playback tests/PUPlayer.Core.Tests/Playback
git commit -m "feat: define player worker protocol"
```

### Task 5: Isolated libmpv worker

**Files:**
- Create: `src/PUPlayer.MpvWorker/Interop/MpvNative.cs`
- Create: `src/PUPlayer.MpvWorker/Interop/MpvLibraryResolver.cs`
- Create: `src/PUPlayer.MpvWorker/Interop/IMpvClient.cs`
- Create: `src/PUPlayer.MpvWorker/Interop/MpvClient.cs`
- Create: `src/PUPlayer.MpvWorker/Worker/WorkerOptions.cs`
- Create: `src/PUPlayer.MpvWorker/Worker/PlayerWorker.cs`
- Modify: `src/PUPlayer.MpvWorker/Program.cs`
- Test: `tests/PUPlayer.IntegrationTests/Worker/PlayerWorkerTests.cs`

**Interfaces:**
- Consumes: `--pipe <name> --token <hex> --wid <uint64> --mpv <absolute dll path>`.
- Produces: `PlayerEvent` messages and applies every `PlayerRequest` to one libmpv handle.

```csharp
public interface IMpvClient : IDisposable
{
    void Load(string path);
    void SetPaused(bool value);
    void Seek(double seconds);
    void SetVolume(double percent);
    void SetSpeed(double value);
    void SetTransform(MpvTransform value);
    PlayerSnapshot ReadSnapshot();
}
```

- [ ] **Step 1: Write failing worker tests with a recording mpv client**

```csharp
[Fact]
public async Task Commands_AreAppliedOnlyToTheOwnedClient()
{
    var mpv = new RecordingMpvClient();
    var worker = new PlayerWorker(mpv);
    await worker.ApplyAsync(new PlayerRequest.SetVolume(1, 175), default);
    await worker.ApplyAsync(new PlayerRequest.Seek(2, 42.5), default);
    Assert.Equal(["volume:175", "seek:42.5"], mpv.Calls);
}

[Fact]
public async Task Shutdown_DisposesMpv()
{
    var mpv = new RecordingMpvClient();
    var worker = new PlayerWorker(mpv);
    await worker.ApplyAsync(new PlayerRequest.Shutdown(1), default);
    Assert.True(mpv.Disposed);
}

sealed class RecordingMpvClient : IMpvClient
{
    public List<string> Calls { get; } = [];
    public bool Disposed { get; private set; }
    public void Load(string path) => Calls.Add($"load:{path}");
    public void SetPaused(bool value) => Calls.Add($"pause:{value}");
    public void Seek(double value) => Calls.Add($"seek:{value}");
    public void SetVolume(double value) => Calls.Add($"volume:{value}");
    public void SetSpeed(double value) => Calls.Add($"speed:{value}");
    public void SetTransform(MpvTransform value) => Calls.Add($"transform:{value}");
    public PlayerSnapshot ReadSnapshot() => new(0, 0, true, 1, 100);
    public void Dispose() => Disposed = true;
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests --filter PlayerWorkerTests`

Expected: FAIL because worker types do not exist.

- [ ] **Step 3: Implement libmpv interop and command mapping**

Register the absolute DLL path before the first native call:

```csharp
internal static class MpvLibraryResolver
{
    public static void Register(string fullPath) => NativeLibrary.SetDllImportResolver(
        typeof(MpvNative).Assembly,
        (name, _, _) => name == "libmpv-2.dll" ? NativeLibrary.Load(fullPath) : nint.Zero);
}
```

Declare only the required client API:

```csharp
internal enum MpvFormat { Flag = 3, Int64 = 4, Double = 5 }

internal static class MpvNative
{
    const string Dll = "libmpv-2.dll";
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern nint mpv_create();
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int mpv_set_option_string(nint ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int mpv_initialize(nint ctx);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern int mpv_command(nint ctx, nint args);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mpv_get_property")]
    internal static extern int mpv_get_double(nint ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, MpvFormat format, ref double value);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "mpv_get_property")]
    internal static extern int mpv_get_flag(nint ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, MpvFormat format, ref int value);
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)] internal static extern void mpv_terminate_destroy(nint ctx);
}
```

`MpvClient.Command(params string[] values)` converts each value with `Marshal.StringToCoTaskMemUTF8`, pins a null-terminated `nint[]`, calls `mpv_command`, and frees every allocation in `finally`. Use commands such as `Command("loadfile", path, "replace")` and `Command("set", "video-zoom", value)`; never concatenate paths into command strings.

Initialize with `wid`, `vo=gpu-next`, `hwdec=auto-safe`, `volume-max=200`, `config=no`, `osc=no`, `input-default-bindings=no`, `input-vo-keyboard=no`, `terminal=no`, `save-position-on-quit=no` and `idle=yes`. Cast the cross-process HWND to unsigned 32-bit decimal before assigning `wid`. Map transforms to `video-zoom`, `video-pan-x` and `video-pan-y`.

Host a `NamedPipeServerStream` whose first line must equal the random token before accepting protocol messages. Validate every deserialized `load` again with `LocalMediaPath.TryCreate` before calling mpv. Emit snapshots at 4 Hz with position, duration, pause, speed and volume.

- [ ] **Step 4: Run worker tests and DLL load smoke check**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests --filter PlayerWorkerTests`

Run: `scripts/dotnet.ps1 build src/PUPlayer.MpvWorker -c Debug`

Run: `Copy-Item vendor/mpv/libmpv-2.dll src/PUPlayer.MpvWorker/bin/Debug/net8.0/`

Run: `scripts/dotnet.ps1 run --project src/PUPlayer.MpvWorker -- --self-test-mpv vendor/mpv/libmpv-2.dll`

Expected: PASS and worker prints only `libmpv: ok`.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.MpvWorker tests/PUPlayer.IntegrationTests/Worker
git commit -m "feat: host libmpv in isolated workers"
```

### Task 6: WPF panel, independent timeline and playback controls

**Files:**
- Create: `src/PUPlayer.App/Playback/IPlayerBackend.cs`
- Create: `src/PUPlayer.App/Playback/MpvWorkerBackend.cs`
- Create: `src/PUPlayer.App/Playback/MpvSurface.cs`
- Create: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Create: `src/PUPlayer.App/Views/PlayerPanelView.xaml`
- Create: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Create: `tests/PUPlayer.IntegrationTests/Fakes/FakePlayerBackend.cs`
- Test: `tests/PUPlayer.IntegrationTests/Playback/PlayerPanelViewModelTests.cs`

**Interfaces:**
- Consumes: one `MediaSlot`, one child HWND and one `IPlayerBackend`.
- Produces: per-panel `PlayPause`, `Seek`, `Skip`, `SetVolume`, `SetSpeed`, `SetTransform`, `Close` commands and observable snapshot properties.

- [ ] **Step 1: Write failing view-model isolation tests**

```csharp
[Fact]
public async Task Seek_ChangesOnlyItsBackend()
{
    var left = new FakeBackend();
    var right = new FakeBackend();
    var a = new PlayerPanelViewModel(left);
    var b = new PlayerPanelViewModel(right);
    await a.SeekAsync(73);
    Assert.Equal(["seek:73"], left.Calls);
    Assert.Empty(right.Calls);
}

[Fact]
public async Task Volume_IsClampedToTwoHundred()
{
    var backend = new FakeBackend();
    await new PlayerPanelViewModel(backend).SetVolumeAsync(250);
    Assert.Equal(["volume:200"], backend.Calls);
}

sealed class FakeBackend : IPlayerBackend
{
    public List<string> Calls { get; } = [];
    public async IAsyncEnumerable<PlayerSnapshot> Snapshots([EnumeratorCancellation] CancellationToken cancellationToken) { yield break; }
    public Task LoadAsync(string path, nint windowHandle, CancellationToken cancellationToken) { Calls.Add($"load:{path}"); return Task.CompletedTask; }
    public Task SetPausedAsync(bool value, CancellationToken cancellationToken) { Calls.Add($"pause:{value}"); return Task.CompletedTask; }
    public Task SeekAsync(double value, CancellationToken cancellationToken) { Calls.Add($"seek:{value}"); return Task.CompletedTask; }
    public Task SetVolumeAsync(double value, CancellationToken cancellationToken) { Calls.Add($"volume:{value}"); return Task.CompletedTask; }
    public Task SetSpeedAsync(double value, CancellationToken cancellationToken) { Calls.Add($"speed:{value}"); return Task.CompletedTask; }
    public Task SetTransformAsync(MpvTransform value, CancellationToken cancellationToken) { Calls.Add($"transform:{value}"); return Task.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests --filter PlayerPanelViewModelTests`

Expected: FAIL because panel types do not exist.

- [ ] **Step 3: Implement the panel and worker backend**

`IPlayerBackend` exposes exact async methods:

```csharp
public interface IPlayerBackend : IAsyncDisposable
{
    IAsyncEnumerable<PlayerSnapshot> Snapshots(CancellationToken cancellationToken);
    Task LoadAsync(string path, nint windowHandle, CancellationToken cancellationToken);
    Task SetPausedAsync(bool value, CancellationToken cancellationToken);
    Task SeekAsync(double seconds, CancellationToken cancellationToken);
    Task SetVolumeAsync(double percent, CancellationToken cancellationToken);
    Task SetSpeedAsync(double speed, CancellationToken cancellationToken);
    Task SetTransformAsync(MpvTransform transform, CancellationToken cancellationToken);
}
```

`MpvSurface : HwndHost` creates a child Win32 `STATIC` window and exposes `HandleReady`. `PlayerPanelView` overlays controls above that surface, updates only from its own snapshot stream and seeks only after the user releases the slider thumb.

- [ ] **Step 4: Run tests and build WPF**

Run: `scripts/dotnet.ps1 test PUPlayer.sln`

Run: `scripts/dotnet.ps1 build src/PUPlayer.App -c Debug`

Expected: PASS with zero warnings.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.App tests/PUPlayer.IntegrationTests/Playback
git commit -m "feat: add independent player panels"
```

### Task 7: Drag-to-mosaic and horizontal/vertical layouts

**Files:**
- Create: `src/PUPlayer.App/ViewModels/WorkspaceViewModel.cs`
- Create: `src/PUPlayer.App/Views/WorkspaceView.xaml`
- Create: `src/PUPlayer.App/Views/WorkspaceView.xaml.cs`
- Modify: `src/PUPlayer.App/MainWindow.xaml`
- Modify: `src/PUPlayer.App/MainWindow.xaml.cs`
- Test: `tests/PUPlayer.IntegrationTests/Workspace/WorkspaceViewModelTests.cs`

**Interfaces:**
- Consumes: Explorer file drops and panel-close requests.
- Produces: one panel, horizontal split or vertical split; third-file result `ReplaceChoiceRequired`.

`WorkspaceViewModel(Func<IPlayerBackend> backendFactory)` creates exactly one backend for every accepted slot.

- [ ] **Step 1: Write failing mosaic behavior tests**

```csharp
[Fact]
public async Task DropSecondFile_CreatesHorizontalSplit()
{
    var vm = new WorkspaceViewModel(() => new FakeBackend());
    await vm.OpenAsync(@"F:\media\one.mp4");
    await vm.DropAsync(@"F:\media\two.mp4");
    Assert.Equal(LayoutMode.SplitHorizontal, vm.Layout);
    Assert.Equal(2, vm.Panels.Count);
}

[Fact]
public async Task DropThirdFile_RequiresExplicitReplacement()
{
    var vm = new WorkspaceViewModel(() => new FakeBackend());
    await vm.OpenAsync("a");
    await vm.DropAsync("b");
    Assert.Equal(DropResult.ReplaceChoiceRequired, await vm.DropAsync("c"));
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests --filter WorkspaceViewModelTests`

Expected: FAIL because workspace view-model types do not exist.

- [ ] **Step 3: Implement drag/drop and layouts**

Accept exactly one dropped local media file per operation. Bind an `ItemsControl` to `Panels`; use a two-column `Grid` for `SplitHorizontal`, a two-row `Grid` for `SplitVertical`, and a single-cell `Grid` for `Single`. Add one toggle button using this command:

```csharp
[RelayCommand]
void ToggleLayout()
{
    State = State.ToggleLayout();
    OnPropertyChanged(nameof(Layout));
}
```

When a panel closes, dispose its backend before removing it. For a third drop, display a modal with the two existing filenames and replace only after the user clicks one.

- [ ] **Step 4: Run tests and WPF build**

Run: `scripts/dotnet.ps1 test PUPlayer.sln`

Run: `scripts/dotnet.ps1 build src/PUPlayer.App -c Debug`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.App tests/PUPlayer.IntegrationTests/Workspace
git commit -m "feat: add two-video mosaic layouts"
```

### Task 8: Manual zoom interactions and focus-scoped shortcuts

**Files:**
- Create: `src/PUPlayer.App/Playback/ZoomInteraction.cs`
- Modify: `src/PUPlayer.App/Views/PlayerPanelView.xaml.cs`
- Modify: `src/PUPlayer.App/ViewModels/PlayerPanelViewModel.cs`
- Test: `tests/PUPlayer.IntegrationTests/Zoom/ZoomInteractionTests.cs`

**Interfaces:**
- Consumes: wheel delta, pointer position, drag delta, double-click and focused-panel shortcuts.
- Produces: a clamped `ZoomState` sent only to the selected panel backend.

- [ ] **Step 1: Write failing interaction tests**

```csharp
[Fact]
public void Wheel_UsesCursorAndNeverExceedsEightTimes()
{
    var interaction = new ZoomInteraction();
    for (var i = 0; i < 30; i++) interaction.Wheel(120, new(.8, .3));
    Assert.Equal(8, interaction.State.Scale);
}

[Fact]
public void DoubleClick_ResetsTransform()
{
    var interaction = new ZoomInteraction();
    interaction.Wheel(120, new(.7, .5));
    interaction.Reset();
    Assert.Equal(ZoomState.Default, interaction.State);
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests --filter ZoomInteractionTests`

Expected: FAIL because interaction types do not exist.

- [ ] **Step 3: Implement pointer and keyboard behavior**

Use a factor of `1.15` per wheel notch. Normalize pointer and drag against the panel's actual dimensions. Send transforms after wheel events and at most every 16 ms during dragging. Double-click sends `ZoomState.Default`. Bind Space, Left, Right, Up and Down to the focused panel only: play/pause, ±10 seconds and ±5 volume points.

```csharp
public void Wheel(int delta, NormalizedPoint cursor) =>
    State = State.ZoomAt(Math.Pow(1.15, delta / 120d), cursor);
public void Drag(double dx, double dy) => State = State.PanBy(dx, dy);
public void Reset() => State = ZoomState.Default;
```

- [ ] **Step 4: Run tests and build**

Run: `scripts/dotnet.ps1 test PUPlayer.sln`

Run: `scripts/dotnet.ps1 build src/PUPlayer.App -c Debug`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/PUPlayer.App/Playback src/PUPlayer.App/Views src/PUPlayer.App/ViewModels tests/PUPlayer.IntegrationTests/Zoom
git commit -m "feat: add per-panel manual zoom"
```

### Task 9: Local-only startup, multi-process smoke tests and handoff

**Files:**
- Modify: `src/PUPlayer.App/App.xaml.cs`
- Create: `tests/PUPlayer.IntegrationTests/App/TestApp.cs`
- Create: `tests/PUPlayer.IntegrationTests/App/TestMedia.cs`
- Create: `tests/PUPlayer.IntegrationTests/App/MultiProcessTests.cs`
- Create: `docs/manual-tests/core-smoke.md`

**Interfaces:**
- Consumes: one absolute media path from Windows file association or “Abrir con”.
- Produces: one independent WPF process per invocation; exit code 2 for invalid input.

- [ ] **Step 1: Write the failing multi-process test**

```csharp
[Fact]
public async Task TwoInvocations_StayAliveAsDifferentProcesses()
{
    using var first = TestApp.Start(TestMedia.OneSecondWave());
    using var second = TestApp.Start(TestMedia.OneSecondWave());
    await Task.WhenAll(first.WaitForReady(), second.WaitForReady());
    Assert.NotEqual(first.Id, second.Id);
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run: `scripts/dotnet.ps1 test tests/PUPlayer.IntegrationTests --filter MultiProcessTests`

Expected: FAIL because startup and test harness do not exist.

- [ ] **Step 3: Implement local-only startup and smoke harness**

Validate the first command-line argument with `LocalMediaPath.TryCreate`; do not create a mutex or reuse an existing process. Start WPF only after validation. When `MainWindow.Loaded` fires, `App` creates and retains a manual-reset named event `PUPlayer.Ready.<pid>` until shutdown. The integration helper writes a one-second PCM WAV under `F:\PUPlayer\work\tests`, launches two app executables, waits for those events and closes both through `CloseMainWindow`.

```csharp
sealed class TestApp : IDisposable
{
    readonly Process process;
    TestApp(Process process) => this.process = process;
    public int Id => process.Id;
    public static TestApp Start(string media) => new(Process.Start(new ProcessStartInfo
    {
        FileName = TestPaths.AppExe,
        ArgumentList = { media },
        UseShellExecute = false
    })!);
    public async Task WaitForReady()
    {
        var limit = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < limit)
        {
            if (EventWaitHandle.TryOpenExisting($"PUPlayer.Ready.{Id}", out var ready)) { ready.Dispose(); return; }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Process {Id} did not become ready.");
    }
    public void Dispose()
    {
        if (!process.HasExited && !process.CloseMainWindow()) process.Kill(true);
        process.Dispose();
    }
}
```

`TestMedia.OneSecondWave()` creates `F:\PUPlayer\work\tests\one-second.wav`, writes a 44-byte PCM header for mono, 16-bit, 8 kHz audio followed by 8,000 zero samples, and returns that absolute path. `TestPaths.AppExe` resolves `src\PUPlayer.App\bin\Debug\net8.0-windows\PUPlayer.App.exe` from the repository root.

Document these manual checks in `docs/manual-tests/core-smoke.md`:

```text
1. Open two local videos from Explorer: two windows and four total app/worker processes.
2. Drag a second video into one window: two panels and simultaneous audio.
3. Seek, pause, change speed and volume in each panel without changing the other.
4. Toggle horizontal/vertical while both continue at their own positions.
5. Zoom, pan and reset each panel independently; resize and enter full screen.
6. Close one panel and verify the remaining panel expands.
7. Confirm no recent-file list, resume file, thumbnail or network connection appears.
```

- [ ] **Step 4: Run complete verification**

Run: `scripts/dotnet.ps1 test PUPlayer.sln -c Release`

Run: `scripts/dotnet.ps1 build PUPlayer.sln -c Release`

Run: `powershell -ExecutionPolicy Bypass -File scripts/verify-bootstrap.ps1`

Expected: all commands exit 0 with zero failed tests and zero warnings.

- [ ] **Step 5: Commit**

```powershell
git add src tests docs/manual-tests
git commit -m "test: verify local multi-process playback"
```

## Referencias técnicas

- mpv embedding, client API and zoom: `https://mpv.io/manual/stable/`
- Windows libmpv builds recommended by mpv: `https://mpv.io/installation/`
- Local .NET SDK installer: `https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script`
