using System.Text.Json;

namespace PUPlayer.Core.Settings;

public sealed class AppSettingsStore : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly string path;
    private readonly FileSystemWatcher watcher;
    private readonly Timer debounce;
    private readonly object gate = new();
    private AppSettings last;
    private bool disposed;

    public AppSettingsStore(string path)
    {
        this.path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(this.path)!);
        last = Read();
        debounce = new(_ => Publish(), null, Timeout.Infinite, Timeout.Infinite);
        watcher = new(Path.GetDirectoryName(this.path)!, Path.GetFileName(this.path)) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite };
        watcher.Changed += ChangedFile;
        watcher.Created += ChangedFile;
        watcher.Renamed += ChangedFile;
        watcher.EnableRaisingEvents = true;
    }

    public event Action<AppSettings>? Changed;
    public AppSettings Load() { lock (gate) return last = Read(); }

    public void Save(AppSettings value)
    {
        value = value.Normalize();
        lock (gate)
        {
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Json));
            File.Move(temporary, path, true);
            last = value;
        }
    }

    private AppSettings Read()
    {
        try { return File.Exists(path) ? (JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? AppSettings.Default).Normalize() : AppSettings.Default; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return AppSettings.Default; }
    }

    private void ChangedFile(object sender, FileSystemEventArgs e) => debounce.Change(60, Timeout.Infinite);
    private void Publish()
    {
        if (disposed) return;
        AppSettings value;
        lock (gate) value = Read();
        if (value == last) return;
        last = value;
        Changed?.Invoke(value);
    }

    public void Dispose()
    {
        disposed = true;
        watcher.Dispose();
        debounce.Dispose();
    }
}
