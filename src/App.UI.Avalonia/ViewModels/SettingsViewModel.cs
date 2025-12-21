using ReactiveUI;

namespace App.UI.Avalonia.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    public string LocalFolder { get => _localFolder; set => this.RaiseAndSetIfChanged(ref _localFolder, value); }
    private string _localFolder = string.Empty;

    public void Save()
    {
        // Persist settings to a simple JSON file or platform settings store
        var settings = new { LocalFolder, Updated = DateTimeOffset.UtcNow };
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync", "ui-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings));
    }
}
