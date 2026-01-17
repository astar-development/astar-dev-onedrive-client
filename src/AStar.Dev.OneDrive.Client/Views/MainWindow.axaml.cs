using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
/// Main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IWindowPreferencesService? _preferencesService;
    private readonly DispatcherTimer? _savePreferencesTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Retrieve the MainWindowViewModel from DI container
        if(App.Services is not null)
        {
            DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
            _preferencesService = App.Services.GetService<IWindowPreferencesService>();

            // Load and apply saved window position
            _ = LoadWindowPreferencesAsync();
        }

        // Save window position when it changes
        PositionChanged += OnPositionChanged;
        PropertyChanged += OnWindowPropertyChanged;

        // Initialize debounce timer for saving preferences (1 second delay)
        _savePreferencesTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _savePreferencesTimer.Tick += async (s, e) =>
        {
            _savePreferencesTimer.Stop();
            await SaveWindowPreferencesAsync();
        };
    }

    private async Task LoadWindowPreferencesAsync()
    {
        if(_preferencesService is null)
        {
            return;
        }

        try
        {
            WindowPreferences? preferences = await _preferencesService.LoadAsync();
            if(preferences is not null)
            {
                // Apply saved preferences
                if(preferences.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else if(preferences.X.HasValue && preferences.Y.HasValue)
                {
                    Position = new PixelPoint((int)preferences.X.Value, (int)preferences.Y.Value);
                    Width = preferences.Width;
                    Height = preferences.Height;
                }
            }
        }
        catch
        {
            // Ignore errors loading preferences - use defaults
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Restart timer - this debounces the save operation
        _savePreferencesTimer?.Stop();
        _savePreferencesTimer?.Start();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property == WindowStateProperty || e.Property == WidthProperty || e.Property == HeightProperty)
        {
            // Restart timer - this debounces the save operation
            _savePreferencesTimer?.Stop();
            _savePreferencesTimer?.Start();
        }
    }

    private async Task SaveWindowPreferencesAsync()
    {
        if(_preferencesService is null)
        {
            return;
        }

        try
        {
            var preferences = new WindowPreferences(
                Id: 1,
                X: WindowState == WindowState.Normal ? Position.X : null,
                Y: WindowState == WindowState.Normal ? Position.Y : null,
                Width: Width,
                Height: Height,
                IsMaximized: WindowState == WindowState.Maximized
            );

            await _preferencesService.SaveAsync(preferences);
        }
        catch
        {
            // Ignore errors saving preferences
        }
    }
}
