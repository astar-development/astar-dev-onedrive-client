// using System.Reactive.Linq;
// using AStar.Dev.OneDrive.Client.Core.Entities;
// using AStar.Dev.OneDrive.Client.Core.Models.Enums;
// using AStar.Dev.OneDrive.Client.FromV3;
// using AStar.Dev.OneDrive.Client.SyncConflicts;
// using Microsoft.Extensions.Logging;
//
// namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.ViewModels;
//
// public class ConflictResolutionViewModelShould
// {
//     [Fact]
//     public void ThrowArgumentNullExceptionWhenAccountIdIsNull()
//     {
//         ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
//         IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
//         ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();
//
//         Exception? exception = Record.Exception(() => new ConflictResolutionViewModel(
//             null!,
//             syncEngine,
//             conflictResolver,
//             logger));
//
//         _ = exception.ShouldNotBeNull();
//         _ = exception.ShouldBeOfType<ArgumentNullException>();
//     }
//
//     [Fact]
//     public void ThrowArgumentExceptionWhenAccountIdIsEmpty()
//     {
//         ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
//         IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
//         ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();
//
//         Exception? exception = Record.Exception(() => new ConflictResolutionViewModel(
//             string.Empty,
//             syncEngine,
//             conflictResolver,
//             logger));
//
//         _ = exception.ShouldNotBeNull();
//         _ = exception.ShouldBeOfType<ArgumentException>();
//     }
//
//     [Fact]
//     public void ThrowArgumentNullExceptionWhenSyncEngineIsNull()
//     {
//         IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
//         ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();
//
//         Exception? exception = Record.Exception(() => new ConflictResolutionViewModel(
//             "test-account",
//             null!,
//             conflictResolver,
//             logger));
//
//         _ = exception.ShouldNotBeNull();
//         _ = exception.ShouldBeOfType<ArgumentNullException>();
//     }
//
//     [Fact]
//     public void ThrowArgumentNullExceptionWhenConflictResolverIsNull()
//     {
//         ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
//         ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();
//
//         Exception? exception = Record.Exception(() => new ConflictResolutionViewModel(
//             "test-account",
//             syncEngine,
//             null!,
//             logger));
//
//         _ = exception.ShouldNotBeNull();
//         _ = exception.ShouldBeOfType<ArgumentNullException>();
//     }
//
//     [Fact]
//     public void ThrowArgumentNullExceptionWhenLoggerIsNull()
//     {
//         ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
//         IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
//
//         Exception? exception = Record.Exception(() => new ConflictResolutionViewModel(
//             "test-account",
//             syncEngine,
//             conflictResolver,
//             null!));
//
//         _ = exception.ShouldNotBeNull();
//         _ = exception.ShouldBeOfType<ArgumentNullException>();
//     }
//
//     [Fact]
//     public async Task LoadConflictsFromSyncEngineOnInitialization()
//     {
//         var conflicts = new List<SyncConflicts.SyncConflict>
//         {
//             CreateTestConflict("file1.txt"),
//             CreateTestConflict("file2.txt")
//         };
//
//         (ConflictResolutionViewModel? viewModel, ISyncEngine? syncEngine, IConflictResolver _, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         // Wait for LoadConflictsCommand to complete (auto-executed on construction)
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.Conflicts.Count.ShouldBe(2);
//         viewModel.HasConflicts.ShouldBeTrue();
//         _ = await syncEngine.Received(1).GetConflictsAsync("test-account", Arg.Any<CancellationToken>());
//     }
//
//     [Fact]
//     public async Task SetStatusMessageWhenConflictsLoaded()
//     {
//         var conflicts = new List<SyncConflicts.SyncConflict> { CreateTestConflict("file1.txt") };
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver _, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.StatusMessage.ShouldContain("1 conflict(s)");
//     }
//
//     [Fact]
//     public async Task SetStatusMessageWhenNoConflictsFound()
//     {
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver _, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel([]);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.StatusMessage.ShouldBe("No conflicts detected.");
//         viewModel.HasConflicts.ShouldBeFalse();
//     }
//
//     [Fact]
//     public async Task ResolveConflictsWithSelectedStrategies()
//     {
//         var conflicts = new List<SyncConflicts.SyncConflict>
//         {
//             CreateTestConflict("file1.txt"),
//             CreateTestConflict("file2.txt")
//         };
//
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver? conflictResolver, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
//         viewModel.Conflicts[1].SelectedStrategy = ConflictResolutionStrategy.KeepRemote;
//
//         _ = await viewModel.ResolveAllCommand.Execute().FirstAsync();
//
//         await conflictResolver.Received(2).ResolveAsync(
//             Arg.Any<SyncConflicts.SyncConflict>(),
//             Arg.Any<ConflictResolutionStrategy>(),
//             Arg.Any<CancellationToken>());
//     }
//
//     [Fact]
//     public async Task SkipConflictsWithNoneStrategy()
//     {
//         var conflicts = new List<SyncConflicts.SyncConflict>
//         {
//             CreateTestConflict("file1.txt"),
//             CreateTestConflict("file2.txt")
//         };
//
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver? conflictResolver, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
//         viewModel.Conflicts[1].SelectedStrategy = ConflictResolutionStrategy.None;
//
//         _ = await viewModel.ResolveAllCommand.Execute().FirstAsync();
//
//         await conflictResolver.Received(1).ResolveAsync(
//             Arg.Any<SyncConflict>(),
//             Arg.Any<ConflictResolutionStrategy>(),
//             Arg.Any<CancellationToken>());
//
//         viewModel.Conflicts.Count.ShouldBe(1);
//         viewModel.StatusMessage.ShouldContain("1 conflict(s)");
//         viewModel.StatusMessage.ShouldContain("Skipped 1");
//     }
//
//     [Fact]
//     public async Task RemoveResolvedConflictsFromCollection()
//     {
//         var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver _, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
//
//         _ = await viewModel.ResolveAllCommand.Execute().FirstAsync();
//
//         viewModel.Conflicts.Count.ShouldBe(0);
//         viewModel.HasConflicts.ShouldBeFalse();
//     }
//
//     [Fact]
//     public async Task SetIsResolvingFlagDuringResolution()
//     {
//         var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver? conflictResolver, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         var isResolvingValues = new List<bool>();
//
//         viewModel.PropertyChanged += (_, e) =>
//         {
//             if(e.PropertyName == nameof(ConflictResolutionViewModel.IsResolving)) isResolvingValues.Add(viewModel.IsResolving);
//         };
//
//         _ = conflictResolver.ResolveAsync(Arg.Any<SyncConflicts.SyncConflict>(), Arg.Any<ConflictResolutionStrategy>(), Arg.Any<CancellationToken>())
//             .Returns(async _ => await Task.Delay(50));
//
//         viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
//
//         _ = await viewModel.ResolveAllCommand.Execute().FirstAsync();
//
//         isResolvingValues.ShouldContain(true);
//         viewModel.IsResolving.ShouldBeFalse();
//     }
//
//     [Fact]
//     public async Task HandleErrorsDuringConflictLoading()
//     {
//         ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
//         IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
//         ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();
//
//         _ = syncEngine.GetConflictsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
//             .Returns<IReadOnlyList<SyncConflicts.SyncConflict>>(_ => throw new InvalidOperationException("Database error"));
//
//         var viewModel = new ConflictResolutionViewModel("test-account", syncEngine, conflictResolver, logger);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.StatusMessage.ShouldContain("Error loading conflicts");
//         viewModel.IsLoading.ShouldBeFalse();
//     }
//
//     [Fact]
//     public async Task HandleErrorsDuringResolution()
//     {
//         var conflicts = new List<SyncConflicts.SyncConflict> { CreateTestConflict("file1.txt") };
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver? conflictResolver, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         _ = conflictResolver.ResolveAsync(Arg.Any<SyncConflicts.SyncConflict>(), Arg.Any<ConflictResolutionStrategy>(), Arg.Any<CancellationToken>())
//             .Returns(_ => throw new IOException("File locked"));
//
//         viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
//
//         _ = await viewModel.ResolveAllCommand.Execute().FirstAsync();
//
//         viewModel.StatusMessage.ShouldContain("Error resolving conflicts");
//         viewModel.IsResolving.ShouldBeFalse();
//     }
//
//     [Fact]
//     public async Task UpdateHasConflictsPropertyWhenCollectionChanges()
//     {
//         var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver _, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel(conflicts);
//
//         await Task.Delay(100, CancellationToken.None);
//
//         viewModel.HasConflicts.ShouldBeTrue();
//
//         viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
//         _ = await viewModel.ResolveAllCommand.Execute().FirstAsync();
//
//         viewModel.HasConflicts.ShouldBeFalse();
//     }
//
//     [Fact]
//     public void ExecuteCancelCommandSuccessfully()
//     {
//         (ConflictResolutionViewModel? viewModel, ISyncEngine _, IConflictResolver _, ILogger<ConflictResolutionViewModel> _) = CreateTestViewModel([]);
//
//         _ = viewModel.CancelCommand.Execute().Subscribe();
//
//         viewModel.StatusMessage.ShouldContain("cancelled");
//     }
//
//     private static SyncConflicts.SyncConflict CreateTestConflict(string filePath) => new(
//         Guid.CreateVersion7().ToString(),
//         "test-account",
//         filePath,
//         DateTime.UtcNow.AddHours(-1),
//         DateTime.UtcNow,
//         1024,
//         2048,
//         DateTime.UtcNow,
//         ConflictResolutionStrategy.None,
//         false
//     );
//
//     private static (ConflictResolutionViewModel ViewModel, ISyncEngine SyncEngine, IConflictResolver ConflictResolver, ILogger<ConflictResolutionViewModel> Logger)
//         CreateTestViewModel(List<Core.Entities.SyncConflict> conflicts)
//     {
//         ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
//         IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
//         ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();
//
//         _ = syncEngine.GetConflictsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
//             .Returns(conflicts);
//
//         _ = conflictResolver.ResolveAsync(Arg.Any<SyncConflicts.SyncConflict>(), Arg.Any<ConflictResolutionStrategy>(), Arg.Any<CancellationToken>())
//             .Returns(Task.CompletedTask);
//
//         var viewModel = new ConflictResolutionViewModel("test-account", syncEngine, conflictResolver, logger);
//
//         return (viewModel, syncEngine, conflictResolver, logger);
//     }
// }
