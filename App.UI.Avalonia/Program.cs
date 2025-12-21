// File: src/App.UI.Avalonia/Program.cs
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Infrastructure.DependencyInjection;
using App.Services.DependencyInjection;
using App.Core.Utilities;
using App.Services;
using App.Infrastructure.DependencyInjection;

class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
