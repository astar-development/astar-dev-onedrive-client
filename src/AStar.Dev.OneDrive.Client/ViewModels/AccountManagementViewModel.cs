using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Client.ViewModels;

/// <summary>
/// ViewModel for managing OneDrive accounts.
/// </summary>
public sealed class AccountManagementViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly IAuthService _authService;
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountManagementViewModel"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="accountRepository">The account repository.</param>
    public AccountManagementViewModel(IAuthService authService, IAccountRepository accountRepository)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(accountRepository);

        _authService = authService;
        _accountRepository = accountRepository;
        Accounts = [];

        // Add Account command - always enabled
        AddAccountCommand = ReactiveCommand.CreateFromTask(
            AddAccountAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        // Remove Account command - enabled when account is selected
        IObservable<bool> canRemove = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null);
        RemoveAccountCommand = ReactiveCommand.CreateFromTask(
            RemoveAccountAsync,
            canRemove,
            RxApp.MainThreadScheduler);

        // Login command - enabled when account is selected and not authenticated
        IObservable<bool> canLogin = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && !account.IsAuthenticated);
        LoginCommand = ReactiveCommand.CreateFromTask(
            LoginAsync,
            canLogin,
            RxApp.MainThreadScheduler);

        // Logout command - enabled when account is selected and authenticated
        IObservable<bool> canLogout = this.WhenAnyValue(x => x.SelectedAccount)
            .Select(account => account is not null && account.IsAuthenticated);
        LogoutCommand = ReactiveCommand.CreateFromTask(
            LogoutAsync,
            canLogout,
            RxApp.MainThreadScheduler);

        // Dispose observables
        _ = canRemove.Subscribe().DisposeWith(_disposables);
        _ = canLogin.Subscribe().DisposeWith(_disposables);
        _ = canLogout.Subscribe().DisposeWith(_disposables);

        // Load accounts on initialization
        _ = LoadAccountsAsync();
    }

    /// <summary>
    /// Gets the collection of accounts.
    /// </summary>
    public ObservableCollection<AccountInfo> Accounts { get; }

    /// <summary>
    /// Gets or sets the transient toast message to show to the user.
    /// </summary>
    public string? ToastMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets whether the toast is currently visible.
    /// </summary>
    public bool ToastVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private CancellationTokenSource? _toastCts;
    /// <summary>
    /// Gets or sets the currently selected account.
    /// </summary>
    public AccountInfo? SelectedAccount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the view is loading data.
    /// </summary>
    public bool IsLoading
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets the command to add a new account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddAccountCommand { get; }

    /// <summary>
    /// Gets the command to remove the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveAccountCommand { get; }

    /// <summary>
    /// Gets the command to login to the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoginCommand { get; }

    /// <summary>
    /// Gets the command to logout from the selected account.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
    /// <summary>
    /// Reloads the accounts from the repository.
    /// </summary>
    public async Task ReloadAccountsAsync() => await LoadAccountsAsync();

    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        try
        {
            IReadOnlyList<AccountInfo> accounts = await _accountRepository.GetAllAsync();
            Accounts.Clear();
            foreach(AccountInfo account in accounts)
                Accounts.Add(account);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddAccountAsync()
    {
        IsLoading = true;
        try
        {
            // Clear any existing toast and start fresh
            if(_toastCts is not null)
            {
                await _toastCts.CancelAsync();
            }

            ToastMessage = null;
            ToastVisible = false;

            AuthenticationResult result = await _authService.LoginAsync();
            if(result is { Success: true, AccountId: not null, DisplayName: not null })
            {
                // Create new account with default sync path
                var defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ApplicationMetadata.ApplicationFolder,
                    result.DisplayName.Replace("@", "-").Replace(".", "-"));

                var newAccount = new AccountInfo(
                    AccountId: result.AccountId,
                    DisplayName: result.DisplayName,
                    LocalSyncPath: defaultPath,
                    IsAuthenticated: true,
                    LastSyncUtc: null,
                    DeltaToken: null,
                    EnableDetailedSyncLogging: false,
                    EnableDebugLogging: false,
                    MaxParallelUpDownloads: 5,
                    MaxItemsInBatch: 50,
                    AutoSyncIntervalMinutes: null);

                await _accountRepository.AddAsync(newAccount);
                Accounts.Add(newAccount);
                SelectedAccount = newAccount;
            }
            else if(result is { Success: false, ErrorMessage: not null })
            {
                _ = ShowToastAsync(result.ErrorMessage);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RemoveAccountAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            await _accountRepository.DeleteAsync(SelectedAccount.AccountId);
            _ = Accounts.Remove(SelectedAccount);
            SelectedAccount = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoginAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            // Clear any existing toast and start fresh
            if(_toastCts is not null)
            {
                await _toastCts.CancelAsync();
            }

            ToastMessage = null;
            ToastVisible = false;

            AuthenticationResult result = await _authService.LoginAsync();
            if(result.Success)
            {
                AccountInfo updatedAccount = SelectedAccount with { IsAuthenticated = true };
                await _accountRepository.UpdateAsync(updatedAccount);

                var index = Accounts.IndexOf(SelectedAccount);
                if(index >= 0)
                {
                    Accounts[index] = updatedAccount;
                    SelectedAccount = updatedAccount;
                }
            }
            else if(result is { Success: false, ErrorMessage: not null })
            {
                _ = ShowToastAsync(result.ErrorMessage);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ShowToastAsync(string message)
    {
        try
        {
            // Clear any existing toast and start fresh
            if(_toastCts is not null)
            {
                await _toastCts.CancelAsync();
            }

            _toastCts = new CancellationTokenSource();
            ToastMessage = message;
            ToastVisible = true;

            await Task.Delay(TimeSpan.FromSeconds(5), _toastCts.Token);

            // Clear toast if not cancelled
            ToastVisible = false;
            ToastMessage = null;
        }
        catch(TaskCanceledException)
        {
            // Ignore - a new toast was requested
        }
        finally
        {
            _toastCts?.Dispose();
            _toastCts = null;
        }
    }

    private async Task LogoutAsync()
    {
        if(SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            var success = await _authService.LogoutAsync(SelectedAccount.AccountId);
            if(success)
            {
                AccountInfo updatedAccount = SelectedAccount with { IsAuthenticated = false };
                await _accountRepository.UpdateAsync(updatedAccount);

                var index = Accounts.IndexOf(SelectedAccount);
                if(index >= 0)
                {
                    Accounts[index] = updatedAccount;
                    SelectedAccount = updatedAccount;
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _disposables.Dispose();
}
