using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for the View Sync History window.
/// </summary>
public sealed class ViewSyncHistoryViewModel : ReactiveObject
{
    private readonly IAccountRepository _accountRepository;
    private readonly IFileOperationLogRepository _fileOperationLogRepository;
    private int _currentPage = 1;
    private const int PageSize = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewSyncHistoryViewModel"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="fileOperationLogRepository">Repository for file operation logs.</param>
    public ViewSyncHistoryViewModel(
        IAccountRepository accountRepository,
        IFileOperationLogRepository fileOperationLogRepository)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(fileOperationLogRepository);

        _accountRepository = accountRepository;
        _fileOperationLogRepository = fileOperationLogRepository;

        Accounts = [];
        SyncHistory = [];

        LoadNextPageCommand = ReactiveCommand.CreateFromTask(LoadNextPageAsync);
        LoadPreviousPageCommand = ReactiveCommand.CreateFromTask(LoadPreviousPageAsync);

        // Load accounts on initialization
        _ = LoadAccountsAsync();
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    /// Gets the collection of sync history records.
    /// </summary>
    public ObservableCollection<FileOperationLog> SyncHistory { get; }

    /// <summary>
    /// Gets or sets the selected account.
    /// </summary>
    public AccountInfo? SelectedAccount
    {
        get;
        set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            if(value is not null)
            {
                _currentPage = 1;
                _ = LoadSyncHistoryAsync();
            }
        }
    }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        private set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    /// <summary>
    /// Gets a value indicating whether there are more records to load.
    /// </summary>
    public bool HasMoreRecords
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    /// <summary>
    /// Gets a value indicating whether data is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether the previous page button should be enabled.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1 && !IsLoading;

    /// <summary>
    /// Gets a value indicating whether the next page button should be enabled.
    /// </summary>
    public bool CanGoToNextPage => HasMoreRecords && !IsLoading;

    /// <summary>
    /// Gets the command to load the next page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadNextPageCommand { get; }

    /// <summary>
    /// Gets the command to load the previous page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadPreviousPageCommand { get; }

    private async Task LoadAccountsAsync()
    {
        try
        {
            IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach(AccountInfo account in accounts) Accounts.Add(account);
        }
        catch
        {
            // Silently fail - user can retry by reopening window
        }
    }

    private async Task LoadSyncHistoryAsync()
    {
        if(SelectedAccount is null)
        {
            SyncHistory.Clear();
            return;
        }

        IsLoading = true;
        try
        {
            var skip = (CurrentPage - 1) * PageSize;
            IReadOnlyList<FileOperationLog> records = await _fileOperationLogRepository.GetByAccountIdAsync(
                SelectedAccount.AccountId,
                PageSize + 1, // Fetch one extra to determine if more records exist
                skip);

            SyncHistory.Clear();
            HasMoreRecords = records.Count > PageSize;

            // Only add PageSize records (exclude the extra one used for hasMore check)
            IEnumerable<FileOperationLog> recordsToShow = HasMoreRecords ? records.Take(PageSize) : records;
            foreach(FileOperationLog? record in recordsToShow) SyncHistory.Add(record);

            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
        }
        catch
        {
            // Silently fail - display will remain empty
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadNextPageAsync()
    {
        if(!HasMoreRecords || SelectedAccount is null) return;

        CurrentPage++;
        await LoadSyncHistoryAsync();
    }

    private async Task LoadPreviousPageAsync()
    {
        if(CurrentPage <= 1 || SelectedAccount is null) return;

        CurrentPage--;
        await LoadSyncHistoryAsync();
    }
}
