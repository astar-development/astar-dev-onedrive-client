using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
