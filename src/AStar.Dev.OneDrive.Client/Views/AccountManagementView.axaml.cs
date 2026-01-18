using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
/// View for managing OneDrive accounts.
/// </summary>
public partial class AccountManagementView : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountManagementView"/> class.
    /// </summary>
    public AccountManagementView(ViewModels.AccountManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Activated += async (_, _) =>
        {
            if(DataContext is ViewModels.AccountManagementViewModel vm)
            {
                await vm.ReloadAccountsAsync();
            }
        };
    }
}
