using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Repositories;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for the Debug Log Viewer window.
/// </summary>
public sealed class DebugLogViewModel : ReactiveObject
{
    private readonly IAccountRepository _accountRepository;
    private readonly IDebugLogRepository _debugLogRepository;
    private const int _pageSize = 50;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugLogViewModel"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="debugLogRepository">Repository for debug log entries.</param>
    public DebugLogViewModel(
        IAccountRepository accountRepository,
        IDebugLogRepository debugLogRepository)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(debugLogRepository);

        _accountRepository = accountRepository;
        _debugLogRepository = debugLogRepository;

        Accounts = [];
        DebugLogs = [];

        LoadNextPageCommand = ReactiveCommand.CreateFromTask(LoadNextPageAsync);
        LoadPreviousPageCommand = ReactiveCommand.CreateFromTask(LoadPreviousPageAsync);
        ClearLogsCommand = ReactiveCommand.CreateFromTask(ClearLogsAsync);

        _ = LoadAccountsAsync();
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    /// Gets the collection of debug log entries.
    /// </summary>
    public ObservableCollection<DebugLogEntry> DebugLogs { get; }

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
                CurrentPage = 1;
                _ = LoadDebugLogsAsync();
            }
        }
    }

    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
        }
    } = 1;

    /// <summary>
    /// Gets a value indicating whether there are more records to load.
    /// </summary>
    public bool HasMoreRecords
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
        }
    } = true;

    /// <summary>
    /// Gets a value indicating whether data is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get;
        private set
        {
            _ = this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
        }
    }

    /// <summary>
    /// Gets the total number of records available.
    /// </summary>
    public int TotalRecordCount
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether the user can navigate to the next page.
    /// </summary>
    public bool CanGoToNextPage => HasMoreRecords && !IsLoading;

    /// <summary>
    /// Gets a value indicating whether the user can navigate to the previous page.
    /// </summary>
    public bool CanGoToPreviousPage => CurrentPage > 1 && !IsLoading;

    /// <summary>
    /// Gets the command to load the next page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadNextPageCommand { get; }

    /// <summary>
    /// Gets the command to load the previous page.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadPreviousPageCommand { get; }

    /// <summary>
    /// Gets the command to clear logs for the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; }

    private async Task LoadAccountsAsync()
    {
        try
        {
            IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach(AccountInfo account in accounts)
            {
                Accounts.Add(account);
            }
        }
        catch(Exception)
        {
            // Log or handle error if needed
        }
    }

    private async Task LoadDebugLogsAsync()
    {
        if(SelectedAccount is null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var skip = (CurrentPage - 1) * _pageSize;

            // Fetch PageSize + 1 to determine if there are more records
            IReadOnlyList<DebugLogEntry> logs = await _debugLogRepository.GetByAccountIdAsync(
                SelectedAccount.AccountId,
                _pageSize + 1,
                skip);

            TotalRecordCount = await _debugLogRepository.GetDebugLogCountByAccountIdAsync(SelectedAccount.AccountId);

            DebugLogs.Clear();

            // If we got more than PageSize, there are more records
            HasMoreRecords = logs.Count > _pageSize;

            // Only show PageSize records
            IEnumerable<DebugLogEntry> logsToDisplay = logs.Take(_pageSize);
            foreach(DebugLogEntry? log in logsToDisplay)
            {
                DebugLogs.Add(log);
            }

            // Update total count estimate based on current position
            if(HasMoreRecords)
            {
                // We know there are at least (CurrentPage * PageSize) + 1 records
                TotalRecordCount = Math.Max(TotalRecordCount, (CurrentPage * _pageSize) + 1);
            }
            else
            {
                // We're on the last page, so we know the exact total
                TotalRecordCount = skip + DebugLogs.Count;
            }

            // Notify property changes for navigation buttons
            this.RaisePropertyChanged(nameof(CanGoToNextPage));
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
        }
        catch(Exception)
        {
            // Log or handle error if needed
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadNextPageAsync()
    {
        if(!HasMoreRecords || SelectedAccount is null)
        {
            return;
        }

        CurrentPage++;
        await LoadDebugLogsAsync();
    }

    private async Task LoadPreviousPageAsync()
    {
        if(CurrentPage <= 1 || SelectedAccount is null)
        {
            return;
        }

        CurrentPage--;
        await LoadDebugLogsAsync();
    }

    private async Task ClearLogsAsync()
    {
        if(SelectedAccount is null)
        {
            return;
        }

        IsLoading = true;
        try
        {
            await _debugLogRepository.DeleteByAccountIdAsync(SelectedAccount.AccountId);
            CurrentPage = 1;
            await LoadDebugLogsAsync();
        }
        catch(Exception)
        {
            // Log or handle error if needed
        }
        finally
        {
            IsLoading = false;
        }
    }
}
