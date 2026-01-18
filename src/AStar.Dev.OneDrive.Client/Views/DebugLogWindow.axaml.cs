using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
/// Window for viewing debug logs.
/// </summary>
public partial class DebugLogWindow : Window
{
    public DebugLogWindow()
    {
        InitializeComponent();

        if(App.Services is not null)
        {
            IAccountRepository accountRepository = App.Services.GetRequiredService<IAccountRepository>();
            IDebugLogRepository debugLogRepository = App.Services.GetRequiredService<IDebugLogRepository>();
            DataContext = new DebugLogViewModel(accountRepository, debugLogRepository);
        }
    }
}
