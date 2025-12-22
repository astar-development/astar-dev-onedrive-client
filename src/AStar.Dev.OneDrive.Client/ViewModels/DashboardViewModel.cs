using System.Collections.ObjectModel;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<string> RecentTransfers { get; } = [];
    public int PendingDownloads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public int PendingUploads { get; set => this.RaiseAndSetIfChanged(ref field, value); }
    public string SyncStatus { get; set => this.RaiseAndSetIfChanged(ref field, value); } = "Idle";
}
