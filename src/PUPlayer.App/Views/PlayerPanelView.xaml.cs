using System.Globalization;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using PUPlayer.App.ViewModels;
using PUPlayer.Core.Audio;
using PUPlayer.Core.Scenes;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.Cache;
using PUPlayer.Core.MediaTools;
using System.IO;

namespace PUPlayer.App.Views;

public partial class PlayerPanelView : UserControl
{
    private readonly Stopwatch dragClock = Stopwatch.StartNew();
    private readonly Stopwatch thumbnailClock = Stopwatch.StartNew();
    private CancellationTokenSource? thumbnailRequest;
    private Point? cropStart;
    private nint surfaceHandle;
    private PlayerPanelViewModel? loadedViewModel;
    public event Action<PlayerPanelView>? FullscreenRequested;
    public event Action? MouseActivity;

    public PlayerPanelView()
    {
        InitializeComponent();
        Surface.ZoomWheel += Surface_ZoomWheel;
        Surface.Dragged += Surface_Dragged;
        Surface.KeyPressed += Surface_KeyPressed;
        Surface.Clicked += Surface_Clicked;
        Surface.DoubleClicked += () => FullscreenRequested?.Invoke(this);
        Surface.MouseMoved += () => MouseActivity?.Invoke();
        DataContextChanged += async (_, _) => await TryLoadAsync();
    }

    private PlayerPanelViewModel? ViewModel => DataContext as PlayerPanelViewModel;

    public void SetFullscreenAppearance(bool fullscreen, bool controlsVisible)
    {
        HeaderRow.Height = fullscreen ? new(0) : new(42);
        HeaderBar.Visibility = fullscreen ? Visibility.Collapsed : Visibility.Visible;
        ControlsRow.Height = !fullscreen || controlsVisible ? GridLength.Auto : new(0);
        ControlsBar.Visibility = !fullscreen || controlsVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Surface_HandleReady(nint handle)
    {
        surfaceHandle = handle;
        await TryLoadAsync();
    }

    private async Task TryLoadAsync()
    {
        if (surfaceHandle == 0 || ViewModel is not { } vm || ReferenceEquals(vm, loadedViewModel)) return;
        loadedViewModel = vm;
        await vm.LoadAsync(surfaceHandle);
    }

    private async void PlayPause_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.PlayPauseAsync(); }
    private async void Back_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.SkipAsync(-10); }
    private async void Forward_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.SkipAsync(10); }
    private async void Timeline_Released(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (ViewModel is { } vm) await vm.SeekAsync(Timeline.Value); }
    private async void Volume_Released(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (ViewModel is { } vm) await vm.SetVolumeAsync(Volume.Value); }
    private void LoopStart_Click(object sender, RoutedEventArgs e) => ViewModel?.SetLoopStart();
    private void LoopEnd_Click(object sender, RoutedEventArgs e) => ViewModel?.SetLoopEnd();
    private void LoopClear_Click(object sender, RoutedEventArgs e) => ViewModel?.ClearLoop();
    private async void AnalyzeScenes_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.AnalyzeScenesAsync(); }
    private void CancelScenes_Click(object sender, RoutedEventArgs e) => ViewModel?.CancelSceneAnalysis();
    private void AddFavorite_Click(object sender, RoutedEventArgs e) => ViewModel?.AddFavorite();
    private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } vm && MarkerList.SelectedItem is SceneMarker marker) vm.RemoveFavorite(marker);
    }
    private async void SceneMarker_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is { } vm && ((ListBox)sender).SelectedItem is SceneMarker marker) await vm.SeekMarkerAsync(marker);
    }

    private async void Speed_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not { } vm || Speed.SelectedItem is not ComboBoxItem { Tag: string value }) return;
        await vm.SetSpeedAsync(double.Parse(value, CultureInfo.InvariantCulture));
    }

    private async void AudioPreset_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is not { } vm || AudioPreset.SelectedItem is not ComboBoxItem { Tag: string value } ||
            !Enum.TryParse<AudioPreset>(value, out var preset)) return;
        await vm.ApplyAudioPresetAsync(preset);
    }

    private async void ManualAudio_Released(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ViewModel is { } vm) await vm.ApplyManualAudioAsync();
    }

    private async void ManualAudio_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } vm) await vm.ApplyManualAudioAsync();
    }

    private async void EnhanceVoice_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } vm) await vm.EnhanceVoiceAsync();
    }

    private async void EnhanceDetail_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } vm) await vm.EnhanceDetailAsync();
    }

    private void CancelVoice_Click(object sender, RoutedEventArgs e) => ViewModel?.CancelVoiceEnhancement();

    private async void OriginalAudio_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } vm) await vm.UseOriginalAudioAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => ViewModel?.RequestClose();

    private async void Tracking_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm) return;
        if (vm.IsTracking) await vm.StopTrackingAsync(); else await vm.StartTrackingAsync();
    }

    private async void Surface_Clicked(double x, double y)
    {
        if (ViewModel is { IsSubjectSelectionRequired: true } vm) await vm.SelectSubjectAsync(new(x, y));
    }

    private async void Surface_ZoomWheel(int delta, double x, double y)
    {
        if (ViewModel is { } vm) await vm.ZoomWheelAsync(delta, new(x, y));
    }

    private async void Surface_Dragged(double dx, double dy)
    {
        if (dragClock.ElapsedMilliseconds < 16 || ViewModel is not { } vm) return;
        dragClock.Restart();
        await vm.PanAsync(dx, dy);
    }

    private async void Surface_KeyPressed(int key)
    {
        if (ViewModel is not { } vm) return;
        switch (key)
        {
            case 0x20: await vm.PlayPauseAsync(); break;
            case 0x25: await vm.SkipAsync(-10); break;
            case 0x27: await vm.SkipAsync(10); break;
            case 0x26: await vm.SetVolumeAsync(vm.VolumePercent + 5); break;
            case 0x28: await vm.SetVolumeAsync(vm.VolumePercent - 5); break;
        }
    }

    private void ClipStart_Click(object sender, RoutedEventArgs e) => ViewModel?.MarkClipStart();
    private void ClipEnd_Click(object sender, RoutedEventArgs e) => ViewModel?.MarkClipEnd();
    private void ClipClear_Click(object sender, RoutedEventArgs e) => ViewModel?.ClearClipMarks();
    private void ClipOriginal_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) vm.ClipMode = ClipExportMode.Original; }
    private void ClipView_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) vm.ClipMode = ClipExportMode.CurrentView; }
    private async void ExportClip_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.ExportClipAsync(); }
    private void CancelClip_Click(object sender, RoutedEventArgs e) => ViewModel?.CancelClipExport();
    private async void RotateLeft_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.RotateLeftAsync(); }
    private async void RotateRight_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.RotateRightAsync(); }
    private async void MirrorX_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.ToggleMirrorXAsync(); }
    private async void MirrorY_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) await vm.ToggleMirrorYAsync(); }
    private void Crop_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) { vm.IsCropEditing = true; ToolsToggle.IsChecked = false; } }
    private async void ResetGeometry_Click(object sender, RoutedEventArgs e) { if (ViewModel is { } vm) { vm.IsCropEditing = false; await vm.ResetGeometryAsync(); } }
    private void RefreshCache_Click(object sender, RoutedEventArgs e) => ViewModel?.RefreshCache();
    private void DeleteAudioCache_Click(object sender, RoutedEventArgs e) => ConfirmCacheDelete(CacheCategory.Audio, "CacheAudioCategory");
    private void DeleteThumbnailCache_Click(object sender, RoutedEventArgs e) => ConfirmCacheDelete(CacheCategory.Thumbnails, "CacheThumbnailsCategory");
    private void DeleteAnalysisCache_Click(object sender, RoutedEventArgs e) => ConfirmCacheDelete(CacheCategory.Analysis, "CacheAnalysisCategory");

    private void ConfirmCacheDelete(CacheCategory category, string categoryKey)
    {
        if (ViewModel is not { } vm) return;
        var bytes = vm.GetCacheReport().Bytes(category);
        var question = string.Format(Text("DeleteCacheQuestion"), Text(categoryKey), FormatBytes(bytes));
        if (MessageBox.Show(question, "IgoonTube", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        ShowDeleteResult(vm.DeleteCache(category));
    }

    private void ShowDeleteResult(CacheDeleteResult result)
    {
        var partial = result.FailedFiles.Count > 0;
        var message = string.Format(Text(partial ? "CacheDeletePartial" : "CacheDeleted"), FormatBytes(result.FreedBytes), result.FailedFiles.Count);
        MessageBox.Show(message, "IgoonTube", MessageBoxButton.OK, partial ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private string Text(string key) => TryFindResource(key)?.ToString() ?? key;
    private static string FormatBytes(long bytes) => bytes < 1024 * 1024 ? $"{bytes / 1024d:0.#} KB" : $"{bytes / 1024d / 1024:0.#} MB";

    private async void Timeline_MouseMove(object sender, MouseEventArgs e)
    {
        if (ViewModel is not { DurationSeconds: > 0 } vm || Timeline.ActualWidth <= 0 || thumbnailClock.ElapsedMilliseconds < 140) return;
        thumbnailClock.Restart();
        var seconds = Math.Clamp(e.GetPosition(Timeline).X / Timeline.ActualWidth, 0, 1) * vm.DurationSeconds;
        thumbnailRequest?.Cancel();
        thumbnailRequest?.Dispose();
        thumbnailRequest = new CancellationTokenSource();
        try
        {
            var path = await vm.GetThumbnailAsync(seconds, thumbnailRequest.Token);
            if (path is null || !File.Exists(path)) return;
            var image = new BitmapImage();
            image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(path!, UriKind.Absolute); image.EndInit(); image.Freeze();
            ThumbnailImage.Source = image;
            ThumbnailTime.Text = TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
            ThumbnailPopup.HorizontalOffset = e.GetPosition(Timeline).X - 96;
            ThumbnailPopup.IsOpen = true;
        }
        catch (OperationCanceledException) { }
    }

    private void Timeline_MouseLeave(object sender, MouseEventArgs e)
    {
        thumbnailRequest?.Cancel();
        ThumbnailPopup.IsOpen = false;
    }

    private void Crop_Begin(object sender, MouseButtonEventArgs e)
    {
        cropStart = e.GetPosition(CropCanvas);
        CropRectangle.Visibility = Visibility.Visible;
        CropCanvas.CaptureMouse();
        UpdateCrop(cropStart.Value, cropStart.Value);
    }

    private void Crop_Move(object sender, MouseEventArgs e)
    {
        if (cropStart is { } start && e.LeftButton == MouseButtonState.Pressed) UpdateCrop(start, e.GetPosition(CropCanvas));
    }

    private async void Crop_End(object sender, MouseButtonEventArgs e)
    {
        if (cropStart is not { } start || ViewModel is not { } vm) return;
        var end = e.GetPosition(CropCanvas);
        CropCanvas.ReleaseMouseCapture(); cropStart = null; vm.IsCropEditing = false;
        var x = Math.Clamp(Math.Min(start.X, end.X) / CropCanvas.ActualWidth, 0, 1);
        var y = Math.Clamp(Math.Min(start.Y, end.Y) / CropCanvas.ActualHeight, 0, 1);
        var width = Math.Clamp(Math.Abs(end.X - start.X) / CropCanvas.ActualWidth, 0, 1 - x);
        var height = Math.Clamp(Math.Abs(end.Y - start.Y) / CropCanvas.ActualHeight, 0, 1 - y);
        CropRectangle.Visibility = Visibility.Collapsed;
        if (width >= .02 && height >= .02) await vm.SetCropAsync(new CropRect(x, y, width, height));
    }

    private void UpdateCrop(Point start, Point end)
    {
        Canvas.SetLeft(CropRectangle, Math.Min(start.X, end.X)); Canvas.SetTop(CropRectangle, Math.Min(start.Y, end.Y));
        CropRectangle.Width = Math.Abs(end.X - start.X); CropRectangle.Height = Math.Abs(end.Y - start.Y);
    }
}

public sealed class MarkerPositionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values is [double seconds, double duration, double width] && duration > 0 ? Math.Clamp(seconds / duration * width, 0, width) : 0d;
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
