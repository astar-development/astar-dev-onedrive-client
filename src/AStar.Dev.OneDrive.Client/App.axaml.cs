using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client;

public partial class App : Application
{
    public static IServiceProvider? Services { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if(Services is null)
                throw new InvalidOperationException("DI Services not initialized. Ensure Program.ConfigureServices sets App.Services before starting.");

            try
            {
                MainWindow window = Services.GetRequiredService<MainWindow>();
                desktop.MainWindow = window;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create MainWindow: {ex}");
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void SetTheme(ThemeVariant? variant) => RequestedThemeVariant = variant ?? ThemeVariant.Default; // Default == Auto
}
