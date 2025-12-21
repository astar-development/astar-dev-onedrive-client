public sealed class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get => _pendingDownloads; set => this.RaiseAndSetIfChanged(ref _pendingDownloads, value); }
    public int PendingUploads { get => _pendingUploads; set => this.RaiseAndSetIfChanged(ref _pendingUploads, value); }
    public string SyncStatus { get => _syncStatus; set => this.RaiseAndSetIfChanged(ref _syncStatus, value); }

    private int _pendingDownloads;
    private int _pendingUploads;
    private string _syncStatus = "Idle";
}
