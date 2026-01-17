using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
/// Window for updating account details.
/// </summary>
public sealed partial class UpdateAccountDetailsWindow : Window
{
    public UpdateAccountDetailsWindow()
    {
        InitializeComponent();

        // Retrieve the UpdateAccountDetailsViewModel from DI container
        if(App.Services is not null)
        {
            UpdateAccountDetailsViewModel viewModel = App.Services.GetRequiredService<UpdateAccountDetailsViewModel>();
            DataContext = viewModel;

            // Wire up RequestClose event to close the window
            viewModel.RequestClose += (_, _) => Close();
        }
    }
}
