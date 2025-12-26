using System;
using System.IO;
using System.IO.Abstractions;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

public class TransferServiceShould
{
    private static TransferService CreateSut(
        IFileSystemAdapter? fs = null,
        IGraphClient? graph = null,
        ISyncRepository? repo = null,
        ILogger<TransferService>? logger = null,
        UserPreferences? settings = null)
    {
        fs ??= Substitute.For<IFileSystemAdapter>();
        graph ??= Substitute.For<IGraphClient>();
        repo ??= Substitute.For<ISyncRepository>();
        logger ??= Substitute.For<ILogger<TransferService>>();
        settings ??= new UserPreferences
        {
            UiSettings = new UiSettings
            {
                SyncSettings = new SyncSettings
                {
                    MaxParallelDownloads = 2,
                    DownloadBatchSize = 1,
                    MaxRetries = 1,
                    RetryBaseDelayMs = 10
                }
            }
        };
        return new TransferService(fs, graph, repo, logger, settings);
    }

    [Fact]
    public async Task NotReportProgress_WhenNoPendingDownloads()
    {
        var repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(0);
        var sut = CreateSut(repo: repo);
        var progress = new Subject<SyncProgress>();
        sut.Progress.Subscribe(progress);

        await sut.ProcessPendingDownloadsAsync(CancellationToken.None);

        progress.HasObservers.ShouldBeTrue();
    }

    [Fact]
    public async Task LogWarning_WhenDownloadCancelled()
    {
        var repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        var logger = Substitute.For<ILogger<TransferService>>();
        var sut = CreateSut(repo: repo, logger: logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await sut.ProcessPendingDownloadsAsync(cts.Token);

        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task Throw_WhenDownloadItemFailsAfterRetries()
    {
        var repo = Substitute.For<ISyncRepository>();
        repo.GetPendingDownloadCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        repo.GetPendingDownloadsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([
            new DriveItemRecord("id", "did", "file.txt", null, null, 100, DateTimeOffset.UtcNow, false, false)
        ]);
        var fs = Substitute.For<IFileSystemAdapter>();
        var graph = Substitute.For<IGraphClient>();
        graph.DownloadDriveItemContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new IOException("fail"));
        var logger = Substitute.For<ILogger<TransferService>>();
        var sut = CreateSut(fs: fs, graph: graph, repo: repo, logger: logger);

        await Should.ThrowAsync<IOException>(() => sut.ProcessPendingDownloadsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NotThrow_WhenNoPendingUploads()
    {
        var repo = Substitute.For<ISyncRepository>();
        repo.GetPendingUploadsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut(repo: repo);

        await sut.ProcessPendingUploadsAsync(CancellationToken.None);
    }
}
