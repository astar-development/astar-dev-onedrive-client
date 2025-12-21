using App.UI.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace App.UI.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
