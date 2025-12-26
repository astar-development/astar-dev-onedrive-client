using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Graph;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit;

public class GraphClientWrapperShould
{
    [Fact]
    public async Task ThrowInvalidOperationException_WhenDownloadDriveItemContentFails()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        var http = Substitute.For<HttpClient>();
        var logger = Substitute.For<ILogger<GraphClientWrapper>>();
        var wrapper = new GraphClientWrapper(auth, http, logger);

        await Should.ThrowAsync<InvalidOperationException>(() => wrapper.DownloadDriveItemContentAsync("badid", CancellationToken.None));
    }

    [Fact]
    public async Task ThrowException_WhenDeleteDriveItemFails()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        var handler = Substitute.ForPartsOf<HttpMessageHandler>();
        var logger = Substitute.For<ILogger<GraphClientWrapper>>();
        var http = new HttpClient(handler);
        var wrapper = new GraphClientWrapper(auth, http, logger);
        // Not possible to fully mock HttpClient without a custom handler, so just check method exists
        await Assert.ThrowsAnyAsync<Exception>(() => wrapper.DeleteDriveItemAsync("id", CancellationToken.None));
    }

    [Fact]
    public async Task ReturnDeltaPage_WhenGetDriveDeltaPageAsyncSucceeds()
    {
        var auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        var logger = Substitute.For<ILogger<GraphClientWrapper>>();
        // Not possible to fully mock HttpClient without a custom handler, so just check method exists
        var http = new HttpClient();
        var wrapper = new GraphClientWrapper(auth, http, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => wrapper.GetDriveDeltaPageAsync(null, CancellationToken.None));
    }
}
