using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
/// Window for viewing sync history.
/// </summary>
public sealed partial class ViewSyncHistoryWindow : Window
{
    public ViewSyncHistoryWindow()
    {
        InitializeComponent();

        // Retrieve dependencies from DI container and create ViewModel
        if(App.Services is not null)
        {
            IAccountRepository accountRepo = App.Services.GetRequiredService<IAccountRepository>();
            IFileOperationLogRepository fileOpLogRepo = App.Services.GetRequiredService<IFileOperationLogRepository>();
            DataContext = new ViewSyncHistoryViewModel(accountRepo, fileOpLogRepo);
        }
    }
}
