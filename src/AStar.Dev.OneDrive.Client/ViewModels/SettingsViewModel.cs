using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    public string LocalFolder { get; set => this.RaiseAndSetIfChanged(ref field, value); } = string.Empty;

    public void Save()
    {
        // Persist settings to a simple JSON file or platform settings store
        var settings = new { LocalFolder, Updated = DateTimeOffset.UtcNow };
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync", "ui-settings.json");
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings));
    }
}
