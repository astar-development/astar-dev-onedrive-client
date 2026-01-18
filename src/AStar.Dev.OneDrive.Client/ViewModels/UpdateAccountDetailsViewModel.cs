using System.Collections.ObjectModel;
using System.Reactive;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for the Update Account Details window.
/// </summary>
public sealed class UpdateAccountDetailsViewModel : ReactiveObject
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAutoSyncSchedulerService _schedulerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAccountDetailsViewModel"/> class.
    /// </summary>
    /// <param name="accountRepository">Repository for account data.</param>
    /// <param name="schedulerService">Service for scheduling automatic syncs.</param>
    public UpdateAccountDetailsViewModel(
        IAccountRepository accountRepository,
        IAutoSyncSchedulerService schedulerService)
    {
        ArgumentNullException.ThrowIfNull(accountRepository);
        ArgumentNullException.ThrowIfNull(schedulerService);
        _accountRepository = accountRepository;
        _schedulerService = schedulerService;

        Accounts = [];

        // Update command - enabled when account is selected and path is valid
        IObservable<bool> canUpdate = this.WhenAnyValue(
            x => x.SelectedAccount,
            x => x.LocalSyncPath,
            (account, path) => account is not null && !string.IsNullOrWhiteSpace(path));

        UpdateCommand = ReactiveCommand.CreateFromTask(UpdateAccountAsync, canUpdate);
        CancelCommand = ReactiveCommand.Create(Cancel);
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolderAsync);

        // Load accounts on initialization
        _ = LoadAccountsAsync();
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

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
                // Load editable fields when account is selected
                LocalSyncPath = value.LocalSyncPath;
                EnableDetailedSyncLogging = value.EnableDetailedSyncLogging;
                EnableDebugLogging = value.EnableDebugLogging;
                MaxParallelUpDownloads = value.MaxParallelUpDownloads;
                MaxItemsInBatch = value.MaxItemsInBatch;
                AutoSyncIntervalMinutes = value.AutoSyncIntervalMinutes;
                StatusMessage = string.Empty;
            }
        }
    }

    /// <summary>
    /// Gets or sets the local sync path.
    /// </summary>
    public string LocalSyncPath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether detailed sync logging is enabled.
    /// </summary>
    public bool EnableDetailedSyncLogging
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled.
    /// </summary>
    public bool EnableDebugLogging
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of parallel upload/download operations (1-10).
    /// </summary>
    public int MaxParallelUpDownloads
    {
        get;
        set
        {
            var clampedValue = Math.Clamp(value, 1, 10);
            _ = this.RaiseAndSetIfChanged(ref field, clampedValue);
        }
    } = 3;

    /// <summary>
    /// Gets or sets the maximum number of items to process in a single batch (1-100).
    /// </summary>
    public int MaxItemsInBatch
    {
        get;
        set
        {
            var clampedValue = Math.Clamp(value, 1, 100);
            _ = this.RaiseAndSetIfChanged(ref field, clampedValue);
        }
    } = 50;

    /// <summary>
    /// Gets or sets the auto-sync interval in minutes (60-1440, null = disabled).
    /// </summary>
    public int? AutoSyncIntervalMinutes
    {
        get;
        set
        {
            var clampedValue = value.HasValue ? Math.Clamp(value.Value, 60, 1440) : (int?)null;
            _ = this.RaiseAndSetIfChanged(ref field, clampedValue);
        }
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the last operation was successful.
    /// </summary>
    public bool IsSuccess
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the command to update the account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> UpdateCommand { get; }

    /// <summary>
    /// Gets the command to cancel and close the window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>
    /// Gets the command to browse for a folder.
    /// </summary>
    public ReactiveCommand<Unit, Unit> BrowseFolderCommand { get; }

    /// <summary>
    /// Event raised when the window should be closed.
    /// </summary>
    public event EventHandler? RequestClose;

    private async Task LoadAccountsAsync()
    {
        try
        {
            IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach(AccountInfo account in accounts)
            {
                AccountInfo? acc = await _accountRepository.GetByIdAsync(account.AccountId);
                AccountInfo accountWithSync = account with { LastSyncUtc = acc?.LastSyncUtc };
                Accounts.Add(accountWithSync);
            }
        }
        catch(Exception ex)
        {
            StatusMessage = $"Failed to load accounts: {ex.Message}";
            IsSuccess = false;
        }
    }

    private async Task UpdateAccountAsync()
    {
        if(SelectedAccount is null) return;

        // Validate LocalSyncPath exists
        if(!Directory.Exists(LocalSyncPath))
        {
            StatusMessage = "Local sync path does not exist. Please select a valid directory.";
            IsSuccess = false;
            return;
        }

        try
        {
            // Create updated account with new values
            AccountInfo updatedAccount = SelectedAccount with
            {
                LocalSyncPath = LocalSyncPath,
                EnableDetailedSyncLogging = EnableDetailedSyncLogging,
                EnableDebugLogging = EnableDebugLogging,
                MaxParallelUpDownloads = MaxParallelUpDownloads,
                MaxItemsInBatch = MaxItemsInBatch,
                AutoSyncIntervalMinutes = AutoSyncIntervalMinutes
            };

            await _accountRepository.UpdateAsync(updatedAccount);

            // Update the scheduler with new auto-sync interval
            _schedulerService.UpdateSchedule(updatedAccount.AccountId, updatedAccount.AutoSyncIntervalMinutes);

            // Update the account in the collection
            var index = Accounts.IndexOf(SelectedAccount);
            if(index >= 0)
            {
                Accounts[index] = updatedAccount;
                SelectedAccount = updatedAccount;
            }

            // Set status AFTER updating SelectedAccount (which clears StatusMessage)
            StatusMessage = "Account updated successfully!";
            IsSuccess = true;

            // Allow UI to update before closing - wait 2 seconds then close on UI thread
            await Task.Delay(2000);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch(Exception ex)
        {
            StatusMessage = $"Failed to update account: {ex.Message}";
            IsSuccess = false;
            // Window stays open on error
        }
    }

    private void Cancel() => RequestClose?.Invoke(this, EventArgs.Empty);

    private async Task BrowseFolderAsync()
    {
        // Get the top level from the current application
        if(Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime { MainWindow.StorageProvider: { } storageProvider })
        {
            IReadOnlyList<IStorageFolder> result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Local Sync Path",
                AllowMultiple = false
            });

            if(result.Count > 0) LocalSyncPath = result[0].Path.LocalPath;
        }
    }
}
