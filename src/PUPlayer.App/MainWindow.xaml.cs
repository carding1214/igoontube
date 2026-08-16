using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using PUPlayer.App.Playback;
using PUPlayer.App.AudioProcessing;
using PUPlayer.App.ViewModels;
using PUPlayer.Core.AudioCache;
using PUPlayer.Core.Playback;
using PUPlayer.App.Tracking;
using PUPlayer.Core.Fullscreen;
using PUPlayer.Core.Scenes;
using PUPlayer.Core.Favorites;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.Cache;
using PUPlayer.Core.Settings;
using PUPlayer.App.Personalization;
using PUPlayer.App.Features;

namespace PUPlayer.App;

public partial class MainWindow : Window
{
    private readonly WorkspaceViewModel workspace;
    private readonly AppSettingsStore settingsStore;
    private readonly SettingsViewModel settings;
    private readonly string mediaPath;
    private bool closing;
    private readonly FullscreenState fullscreen = new();
    private readonly DispatcherTimer fullscreenTimer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private WindowStyle previousStyle;
    private WindowState previousState;
    private ResizeMode previousResizeMode;
    private Rect previousBounds;
    private bool previousTopmost;

    public MainWindow(string mediaPath, PlayerLoadMetrics? metrics = null)
    {
        this.mediaPath = mediaPath;
        var root = FindRuntimeRoot();
        var ffmpeg = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpeg)) ffmpeg = Path.Combine(root, "ffmpeg.exe");
        var store = new SceneIndexStore();
        var cache = new IgoonTubeCacheManager(Path.Combine(root, "data", "cache", "audio"));
        settingsStore = new(Path.Combine(root, "data", "settings.json"));
        var localization = new LocalizationService(Application.Current.Resources);
        var theme = new ThemeService(Application.Current.Resources);
        WorkspaceViewModel? liveWorkspace = null;
        settings = new(settingsStore, value => { localization.Apply(value.Language); theme.Apply(value); liveWorkspace?.Relocalize(); },
            action => { if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action); });
        workspace = new(() => new MpvWorkerBackend(), sceneStore: store, favoriteStore: new FavoriteStore(), cacheManager: cache,
            settings: settings, text: localization, clipDestinationPicker: new SaveFileClipDestinationPicker(),
            featureFactory: () => CreateFeatures(ffmpeg, store), metrics: metrics);
        liveWorkspace = workspace;
        InitializeComponent();
        DataContext = workspace;
        Loaded += Window_Loaded;
        Workspace.FullscreenChanged += Workspace_FullscreenChanged;
        Workspace.FullscreenMouseActivity += ShowFullscreenControls;
        fullscreenTimer.Tick += FullscreenTimer_Tick;
        PreviewKeyDown += Window_KeyDown;
        StateChanged += Window_StateChanged;
    }

    private void Workspace_FullscreenChanged(bool active)
    {
        if (active)
        {
            previousStyle = WindowStyle;
            previousState = WindowState;
            previousResizeMode = ResizeMode;
            previousBounds = RestoreBounds;
            previousTopmost = Topmost;
            fullscreen.Enter(DateTimeOffset.UtcNow);
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            FillCurrentMonitor();
            fullscreenTimer.Start();
            ShowFullscreenControls();
        }
        else RestoreWindow();
    }

    private void ShowFullscreenControls()
    {
        if (!fullscreen.IsActive) return;
        fullscreen.Move(DateTimeOffset.UtcNow);
        Cursor = Cursors.Arrow;
        Workspace.SetFullscreenControls(true);
    }

    private void FullscreenTimer_Tick(object? sender, EventArgs e)
    {
        fullscreen.Tick(DateTimeOffset.UtcNow);
        if (fullscreen.AreControlsVisible) return;
        Cursor = Cursors.None;
        Workspace.SetFullscreenControls(false);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.M && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            await workspace.ActivatePrivacyAsync();
            WindowState = WindowState.Minimized;
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Escape || !fullscreen.IsActive) return;
        Workspace.ExitFullscreen();
        e.Handled = true;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized && workspace.IsPrivate) workspace.RevealNames();
    }

    private void RestoreWindow()
    {
        if (!fullscreen.IsActive) return;
        fullscreen.Exit();
        fullscreenTimer.Stop();
        Cursor = Cursors.Arrow;
        WindowState = WindowState.Normal;
        WindowStyle = previousStyle;
        ResizeMode = previousResizeMode;
        Topmost = previousTopmost;
        Left = previousBounds.Left;
        Top = previousBounds.Top;
        Width = previousBounds.Width;
        Height = previousBounds.Height;
        WindowState = previousState;
    }

    private void FillCurrentMonitor()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(handle, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;
        SetWindowPos(handle, new(-1), info.Monitor.Left, info.Monitor.Top,
            info.Monitor.Right - info.Monitor.Left, info.Monitor.Bottom - info.Monitor.Top, 0x0040 | 0x0020);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }

    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint handle, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(nint handle, nint insertAfter, int x, int y, int width, int height, uint flags);

    private static AudioSeparationService CreateAudioService()
    {
        var root = FindRuntimeRoot();
        var python = FindPython(root);
        var ffmpeg = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpeg)) ffmpeg = Path.Combine(root, "ffmpeg.exe");
        var temp = Path.Combine(root, "work", "audio-processing");
        Directory.CreateDirectory(temp);
        var environment = new Dictionary<string, string>
        {
            ["TORCH_HOME"] = Path.Combine(root, "data", "models", "torch"),
            ["XDG_CACHE_HOME"] = Path.Combine(root, "data", "cache", "python"),
            ["TEMP"] = temp,
            ["TMP"] = temp,
            ["PYTHONDONTWRITEBYTECODE"] = "1",
            ["PATH"] = $"{Path.GetDirectoryName(ffmpeg)};{Environment.GetEnvironmentVariable("PATH")}"
        };
        var runner = new ProcessRunner();
        return new(new AudioCacheLocator(Path.Combine(root, "data", "cache", "audio")), runner,
            new FfmpegAudioEncoder(ffmpeg, runner), python, Path.Combine(temp, "jobs"), environment);
    }

    private static MediaPipeVisionDetector CreateVisionDetector()
    {
        var root = FindRuntimeRoot();
        return new(
            FindPython(root),
            Path.Combine(root, "scripts", "vision_host.py"),
            Path.Combine(root, "data", "models", "mediapipe", "pose_landmarker_lite.task"));
    }

    private static PlayerFeatureFactories CreateFeatures(string ffmpeg, SceneIndexStore store)
    {
#if IGOONTUBE_NO_AI
        return new(static () => null, static () => null, static () => null,
            () => new FfmpegClipExportService(ffmpeg), () => new FfmpegThumbnailService(ffmpeg));
#else
        PlayerFeatureFactories? features = null;
        return features = new(CreateAudioService, CreateVisionDetector,
            () => CreateSceneService(features!.Audio!, store),
            () => new FfmpegClipExportService(ffmpeg),
            () => new FfmpegThumbnailService(ffmpeg));
#endif
    }

    private static SceneAnalysisService CreateSceneService(IAudioSeparationService audio, SceneIndexStore store)
    {
        var root = FindRuntimeRoot();
        var ffmpeg = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpeg)) ffmpeg = Path.Combine(root, "ffmpeg.exe");
        return new(audio, new ProcessRunner(), ffmpeg, store);
    }

    private static string FindRuntimeRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, ".tools", "ai", "python", "python.exe")) ||
                File.Exists(Path.Combine(directory.FullName, ".tools", "ai", ".venv", "Scripts", "python.exe")))
                return directory.FullName;
        return @"F:\PUPlayer";
    }

    private static string FindPython(string root)
    {
        var portable = Path.Combine(root, ".tools", "ai", "python", "python.exe");
        return File.Exists(portable) ? portable : Path.Combine(root, ".tools", "ai", ".venv", "Scripts", "python.exe");
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (closing) { base.OnClosing(e); return; }
        e.Cancel = true;
        await workspace.DisposeAsync();
        settings.Dispose();
        settingsStore.Dispose();
        closing = true;
        _ = Dispatcher.BeginInvoke(Close);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"{Path.GetFileName(mediaPath)} — IgoonTube";
        await workspace.OpenAsync(mediaPath);
    }
}
