using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.FromV3;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using AStar.Dev.OneDrive.Client.ViewModels;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;

public class SyncTreeViewModelShould : IDisposable
{
    [Fact]
    public void ExposeEstimatedSecondsRemainingFromSyncState()
    {
        var syncState = new SyncState(
            AccountId: "acc1",
            Status: SyncStatus.Running,
            TotalFiles: 10,
            CompletedFiles: 5,
            TotalBytes: 1000,
            CompletedBytes: 500,
            FilesDownloading: 2,
            FilesUploading: 1,
            FilesDeleted: 0,
            ConflictsDetected: 0,
            MegabytesPerSecond: 3.5,
            EstimatedSecondsRemaining: 42,
            CurrentScanningFolder: null,
            LastUpdateUtc: null);
        _progressSubject.OnNext(syncState);
        _viewModel.EstimatedSecondsRemaining.ShouldBe(42);
    }

    [Fact]
    public void ExposeMegabytesPerSecondFromSyncState()
    {
        var syncState = new SyncState(
            AccountId: "acc1",
            Status: SyncStatus.Running,
            TotalFiles: 10,
            CompletedFiles: 5,
            TotalBytes: 1000,
            CompletedBytes: 500,
            FilesDownloading: 2,
            FilesUploading: 1,
            FilesDeleted: 0,
            ConflictsDetected: 0,
            MegabytesPerSecond: 7.25,
            EstimatedSecondsRemaining: 10,
            CurrentScanningFolder: null,
            LastUpdateUtc: null);
        _progressSubject.OnNext(syncState);
        _viewModel.MegabytesPerSecond.ShouldBe(7.25);
    }
    private readonly IFolderTreeService _mockFolderService;
    private readonly ISyncSelectionService _mockSelectionService;
    private readonly ISyncEngine _mockSyncEngine;
    private readonly Subject<SyncState> _progressSubject;
    private readonly SyncTreeViewModel _viewModel;
    private bool disposedValue;

    public SyncTreeViewModelShould()
    {
        _mockFolderService = Substitute.For<IFolderTreeService>();
        _mockSelectionService = Substitute.For<ISyncSelectionService>();
        _mockSyncEngine = Substitute.For<ISyncEngine>();

        _progressSubject = new Subject<SyncState>();
        _ = _mockSyncEngine.Progress.Returns(_progressSubject);

        _viewModel = new SyncTreeViewModel(_mockFolderService, _mockSelectionService, _mockSyncEngine);
    }

    [Fact]
    public void InitializeWithEmptyRootFolders()
    {
        _viewModel.RootFolders.ShouldBeEmpty();
        _viewModel.IsLoading.ShouldBeFalse();
        _viewModel.ErrorMessage.ShouldBeNull();
        _viewModel.SelectedAccountId.ShouldBeNull();
    }

    [Fact]
    public async Task LoadRootFoldersWhenAccountIdIsSet()
    {
        var folders = new List<OneDriveFolderNode>
        {
            new() { Id = "folder1", Name = "Folder 1", IsFolder = true },
            new() { Id = "folder2", Name = "Folder 2", IsFolder = true }
        };

        _ = _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(folders);

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(100, CancellationToken.None); // Allow async command to execute

        _viewModel.RootFolders.Count.ShouldBe(2);
        _viewModel.RootFolders[0].Name.ShouldBe("Folder 1");
        _viewModel.RootFolders[1].Name.ShouldBe("Folder 2");
    }

    [Fact]
    public void ClearRootFoldersWhenAccountIdIsNull()
    {
        _viewModel.RootFolders.Add(new OneDriveFolderNode { Id = "test", Name = "Test" });

        _viewModel.SelectedAccountId = null;
        _ = _viewModel.LoadFoldersCommand.Execute().Subscribe();

        _viewModel.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetIsLoadingDuringFolderLoad()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<OneDriveFolderNode>>();
        _ = _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(info => tcs.Task);

        var isLoadingValues = new List<bool>();
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(SyncTreeViewModel.IsLoading))
            {
                isLoadingValues.Add(_viewModel.IsLoading);
            }
        };

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(50, CancellationToken.None);
        isLoadingValues.ShouldContain(true);

        tcs.SetResult([]);
        await Task.Delay(50, CancellationToken.None);
        isLoadingValues.ShouldContain(false);
    }

    [Fact]
    public async Task SetErrorMessageWhenLoadFails()
    {
        _ = _mockFolderService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OneDriveFolderNode>>(new InvalidOperationException("Test error")));

        _viewModel.SelectedAccountId = "account123";

        await Task.Delay(100, CancellationToken.None);

        _ = _viewModel.ErrorMessage.ShouldNotBeNull();
        _viewModel.ErrorMessage.ShouldContain("Test error");
    }

    [Fact]
    public async Task LoadChildrenWhenCommandExecuted()
    {
        var parent = new OneDriveFolderNode { Id = "parent", Name = "Parent", IsFolder = true };
        var children = new List<OneDriveFolderNode>
        {
            new() { Id = "child1", Name = "Child 1", ParentId = "parent", IsFolder = true }
        };

        _ = _mockFolderService.GetChildFoldersAsync("account123", "parent", Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(children);

        _viewModel.SelectedAccountId = "account123";

        _ = _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100, CancellationToken.None);

        parent.Children.Count.ShouldBe(1);
        parent.Children[0].Name.ShouldBe("Child 1");
        parent.ChildrenLoaded.ShouldBeTrue();
    }

    [Fact]
    public async Task NotLoadChildrenIfAlreadyLoaded()
    {
        var parent = new OneDriveFolderNode
        {
            Id = "parent",
            Name = "Parent",
            IsFolder = true,
            ChildrenLoaded = true
        };

        _viewModel.SelectedAccountId = "account123";

        _ = _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100, CancellationToken.None);

        _ = await _mockFolderService.DidNotReceive().GetChildFoldersAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InheritParentSelectionStateForNewChildren()
    {
        var parent = new OneDriveFolderNode
        {
            Id = "parent",
            Name = "Parent",
            IsFolder = true,
            SelectionState = SelectionState.Checked
        };

        var children = new List<OneDriveFolderNode>
        {
            new() { Id = "child1", Name = "Child 1", ParentId = "parent", IsFolder = true }
        };

        _ = _mockFolderService.GetChildFoldersAsync("account123", "parent", Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(children);

        _viewModel.SelectedAccountId = "account123";

        _ = _viewModel.LoadChildrenCommand.Execute(parent).Subscribe();
        await Task.Delay(100, CancellationToken.None);

        parent.Children[0].SelectionState.ShouldBe(SelectionState.Unchecked);
        parent.Children[0].IsSelected.ShouldBeNull();
    }

    [Fact]
    public void ToggleSelectionFromUncheckedToChecked()
    {
        var folder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Folder 1",
            SelectionState = SelectionState.Unchecked
        };

        _ = _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, true);
        _mockSelectionService.Received(1).UpdateParentStates(folder, Arg.Any<List<OneDriveFolderNode>>());
    }

    [Fact]
    public void ToggleSelectionFromCheckedToUnchecked()
    {
        var folder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Folder 1",
            SelectionState = SelectionState.Checked
        };

        _ = _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, false);
    }

    [Fact]
    public void ToggleSelectionFromIndeterminateToChecked()
    {
        var folder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Folder 1",
            SelectionState = SelectionState.Indeterminate
        };

        _ = _viewModel.ToggleSelectionCommand.Execute(folder).Subscribe();

        _mockSelectionService.Received(1).SetSelection(folder, true);
    }

    [Fact]
    public void ClearAllSelectionsWhenCommandExecuted()
    {
        _viewModel.RootFolders.Add(new OneDriveFolderNode { Id = "folder1", Name = "Folder 1" });

        _ = _viewModel.ClearSelectionsCommand.Execute().Subscribe();

        _mockSelectionService.Received(1).ClearAllSelections(Arg.Any<List<OneDriveFolderNode>>());
    }

    [Fact]
    public void GetSelectedFoldersFromSelectionService()
    {
        var selectedFolders = new List<OneDriveFolderNode>
        {
            new() { Id = "selected1", Name = "Selected 1", SelectionState = SelectionState.Checked }
        };

        _ = _mockSelectionService.GetSelectedFolders(Arg.Any<List<OneDriveFolderNode>>())
            .Returns(selectedFolders);

        List<OneDriveFolderNode> result = _viewModel.GetSelectedFolders();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Selected 1");
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountIdChanges()
    {
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(SyncTreeViewModel.SelectedAccountId))
            {
                propertyChangedRaised = true;
            }
        };

        _viewModel.SelectedAccountId = "newAccount";

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void RaiseCollectionChangedWhenRootFoldersModified()
    {
        var collectionChangedRaised = false;
        _viewModel.RootFolders.CollectionChanged += (_, _) => collectionChangedRaised = true;

        _viewModel.RootFolders.Add(new OneDriveFolderNode());

        collectionChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenFolderServiceIsNull()
    {
        Exception? exception = Record.Exception(() => new SyncTreeViewModel(null!, _mockSelectionService, _mockSyncEngine));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSelectionServiceIsNull()
    {
        Exception? exception = Record.Exception(() => new SyncTreeViewModel(_mockFolderService, null!, _mockSyncEngine));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenLoadingChildrenWithNullFolder()
    {
        Exception? exception = Record.Exception(() => _viewModel.LoadChildrenCommand.Execute(null!).Subscribe());

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenTogglingNullFolder()
    {
        Exception? exception = Record.Exception(() => _viewModel.ToggleSelectionCommand.Execute(null!).Subscribe());

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if(!disposedValue)
        {
            if(disposing)
            {
                _progressSubject.Dispose();
                _viewModel.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
