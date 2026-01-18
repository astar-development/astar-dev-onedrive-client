using System.Collections.Specialized;
using AStar.Dev.OneDrive.Client.Models;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.Views;

/// <summary>
/// View for displaying and managing the OneDrive folder sync tree.
/// </summary>
public partial class SyncTreeView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncTreeView"/> class.
    /// </summary>
    public SyncTreeView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if(DataContext is SyncTreeViewModel viewModel)
        {
            // Subscribe to RootFolders collection changes to attach property change handlers
            viewModel.RootFolders.CollectionChanged += OnRootFoldersChanged;

            // Attach to existing items
            foreach(OneDriveFolderNode node in viewModel.RootFolders) AttachNodeExpansionHandler(node, viewModel);
        }
    }

    private void OnRootFoldersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if(DataContext is not SyncTreeViewModel viewModel) return;

        if(e.NewItems != null)
            foreach(OneDriveFolderNode node in e.NewItems) AttachNodeExpansionHandler(node, viewModel);
    }

    private static void AttachNodeExpansionHandler(OneDriveFolderNode node, SyncTreeViewModel viewModel)
    {
        node.PropertyChanged += (s, e) =>
        {
            if(e.PropertyName == nameof(OneDriveFolderNode.IsExpanded) &&
               node is { IsExpanded: true, ChildrenLoaded: false })
            {
                // Trigger lazy loading when expanded for the first time
                _ = viewModel.LoadChildrenCommand.Execute(node).Subscribe();
            }
        };

        // Recursively attach to children as they're added
        node.Children.CollectionChanged += (s, e) =>
        {
            if(e.NewItems != null)
                foreach(OneDriveFolderNode child in e.NewItems) AttachNodeExpansionHandler(child, viewModel);
        };
    }
}
