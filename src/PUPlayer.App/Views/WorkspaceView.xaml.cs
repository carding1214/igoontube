using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using PUPlayer.App.ViewModels;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Cache;

namespace PUPlayer.App.Views;

public partial class WorkspaceView : UserControl
{
    private readonly Dictionary<PlayerPanelViewModel, PlayerPanelView> views = [];
    private WorkspaceViewModel? subscribed;
    private PlayerPanelView? fullscreen;

    public event Action<bool>? FullscreenChanged;
    public event Action? FullscreenMouseActivity;

    public WorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
        Unloaded += (_, _) => Unsubscribe();
    }
    private WorkspaceViewModel? ViewModel => DataContext as WorkspaceViewModel;

    public void ExitFullscreen()
    {
        if (fullscreen is null) return;
        fullscreen.SetFullscreenAppearance(false, true);
        fullscreen = null;
        TopBar.Visibility = Visibility.Visible;
        foreach (var view in views.Values) view.Visibility = Visibility.Visible;
        LayoutPanels();
        FullscreenChanged?.Invoke(false);
    }

    public void SetFullscreenControls(bool visible) => fullscreen?.SetFullscreenAppearance(true, visible);

    private void EnterFullscreen(PlayerPanelView view)
    {
        if (ReferenceEquals(fullscreen, view)) { ExitFullscreen(); return; }
        fullscreen = view;
        TopBar.Visibility = Visibility.Collapsed;
        foreach (var item in views.Values) item.Visibility = ReferenceEquals(item, view) ? Visibility.Visible : Visibility.Collapsed;
        PanelsHost.RowDefinitions.Clear();
        PanelsHost.ColumnDefinitions.Clear();
        Grid.SetRow(view, 0); Grid.SetColumn(view, 0);
        view.SetFullscreenAppearance(true, true);
        FullscreenChanged?.Invoke(true);
    }

    private void Subscribe()
    {
        Unsubscribe();
        subscribed = ViewModel;
        if (subscribed is null) return;
        subscribed.Panels.CollectionChanged += Panels_Changed;
        subscribed.PropertyChanged += ViewModel_PropertyChanged;
        SyncPanels();
    }

    private void Unsubscribe()
    {
        if (subscribed is not null)
        {
            subscribed.Panels.CollectionChanged -= Panels_Changed;
            subscribed.PropertyChanged -= ViewModel_PropertyChanged;
        }
        subscribed = null;
    }

    private void Panels_Changed(object? sender, NotifyCollectionChangedEventArgs e) => SyncPanels();
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorkspaceViewModel.Layout) or nameof(WorkspaceViewModel.Rows) or nameof(WorkspaceViewModel.Columns)) LayoutPanels();
    }

    private void SyncPanels()
    {
        if (ViewModel is not { } vm) return;
        foreach (var pair in views.Where(pair => !vm.Panels.Contains(pair.Key)).ToArray())
        {
            pair.Value.FullscreenRequested -= Panel_FullscreenRequested;
            pair.Value.MouseActivity -= Panel_MouseActivity;
            PanelsHost.Children.Remove(pair.Value);
            views.Remove(pair.Key);
        }
        foreach (var panel in vm.Panels)
        {
            if (views.ContainsKey(panel)) continue;
            var view = new PlayerPanelView { DataContext = panel, Margin = new(2) };
            view.FullscreenRequested += Panel_FullscreenRequested;
            view.MouseActivity += Panel_MouseActivity;
            views.Add(panel, view);
            PanelsHost.Children.Add(view);
        }
        LayoutPanels();
    }

    private void LayoutPanels()
    {
        if (fullscreen is not null || ViewModel is not { } vm) return;
        PanelsHost.RowDefinitions.Clear();
        PanelsHost.ColumnDefinitions.Clear();
        var vertical = vm.Layout == PUPlayer.Core.Workspace.LayoutMode.SplitVertical && views.Count > 1;
        var count = Math.Max(views.Count, 1);
        for (var i = 0; i < (vertical ? count : 1); i++) PanelsHost.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
        for (var i = 0; i < (vertical ? 1 : count); i++) PanelsHost.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        for (var i = 0; i < vm.Panels.Count; i++)
        {
            var view = views[vm.Panels[i]];
            Grid.SetRow(view, vertical ? i : 0);
            Grid.SetColumn(view, vertical ? 0 : i);
        }
    }

    private void Panel_FullscreenRequested(PlayerPanelView view) => EnterFullscreen(view);
    private void Panel_MouseActivity()
    {
        if (fullscreen is not null) FullscreenMouseActivity?.Invoke();
    }

    private void Workspace_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = TryGetSingleLocalFile(e.Data, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Workspace_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel is not { } vm || !TryGetSingleLocalFile(e.Data, out var path)) return;
        if (await vm.DropAsync(path) != DropResult.ReplaceChoiceRequired) return;
        var choice = MessageBox.Show(
            $"Sí: reemplazar {vm.Panels[0].DisplayName}\nNo: reemplazar {vm.Panels[1].DisplayName}",
            "Elige qué video reemplazar", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (choice is MessageBoxResult.Yes or MessageBoxResult.No)
            await vm.ReplaceAsync(choice == MessageBoxResult.Yes ? 0 : 1, path);
    }

    private void ToggleLayout_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleLayout();
    private void AccentInput_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (ViewModel?.Settings is not { } settings) return;
        settings.AccentColor = AccentInput.Text;
        SettingsError.Text = settings.ErrorKey is { } key ? FindResource(key)?.ToString() ?? key : "";
    }
    private void RestorePreset_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.Settings?.RestorePreset();
        SettingsError.Text = "";
    }
    private void GlobalCache_Open(object sender, RoutedEventArgs e) => ViewModel?.RefreshGlobalCache();
    private void GlobalCache_Refresh(object sender, RoutedEventArgs e) => ViewModel?.RefreshGlobalCache();
    private void GlobalCache_DeleteAudio(object sender, RoutedEventArgs e)
    {
        ConfirmGlobalCacheDelete(CacheCategory.Audio, "CacheAudioCategory", MessageBoxImage.Question);
    }
    private void GlobalCache_DeleteAll(object sender, RoutedEventArgs e)
    {
        ConfirmGlobalCacheDelete(CacheCategory.All, "CacheAllCategory", MessageBoxImage.Warning);
    }

    private void ConfirmGlobalCacheDelete(CacheCategory category, string categoryKey, MessageBoxImage icon)
    {
        if (ViewModel is not { } vm) return;
        var bytes = vm.GetGlobalCacheReport().Bytes(category);
        var question = string.Format(Text("DeleteCacheQuestion"), Text(categoryKey), FormatBytes(bytes));
        if (MessageBox.Show(question, "IgoonTube", MessageBoxButton.YesNo, icon) != MessageBoxResult.Yes) return;
        var result = vm.DeleteGlobalCache(category);
        var partial = result.FailedFiles.Count > 0;
        var message = string.Format(Text(partial ? "CacheDeletePartial" : "CacheDeleted"), FormatBytes(result.FreedBytes), result.FailedFiles.Count);
        MessageBox.Show(message, "IgoonTube", MessageBoxButton.OK, partial ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private string Text(string key) => TryFindResource(key)?.ToString() ?? key;
    private static string FormatBytes(long bytes) => bytes < 1024 * 1024 ? $"{bytes / 1024d:0.#} KB" : $"{bytes / 1024d / 1024:0.#} MB";

    private static bool TryGetSingleLocalFile(IDataObject data, out string path)
    {
        path = string.Empty;
        if (!data.GetDataPresent(DataFormats.FileDrop) || data.GetData(DataFormats.FileDrop) is not string[] { Length: 1 } files) return false;
        path = files[0];
        return LocalMediaPath.TryCreate(path, out _);
    }
}
