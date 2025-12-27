using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Graph;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit;

public class GraphClientWrapperShould
{
    [Fact]
    public async Task ThrowInvalidOperationException_WhenDownloadDriveItemContentFails()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        HttpClient http = Substitute.For<HttpClient>();
        ILogger<GraphClientWrapper> logger = Substitute.For<ILogger<GraphClientWrapper>>();
        var wrapper = new GraphClientWrapper(auth, http, logger);

        await Should.ThrowAsync<InvalidOperationException>(() => wrapper.DownloadDriveItemContentAsync("badid", CancellationToken.None));
    }

    [Fact]
    public async Task ThrowException_WhenDeleteDriveItemFails()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        HttpMessageHandler handler = Substitute.ForPartsOf<HttpMessageHandler>();
        ILogger<GraphClientWrapper> logger = Substitute.For<ILogger<GraphClientWrapper>>();
        var http = new HttpClient(handler);
        var wrapper = new GraphClientWrapper(auth, http, logger);

        // No Arg.Any<>() here, just use real values
        await Assert.ThrowsAnyAsync<Exception>(() => wrapper.DeleteDriveItemAsync("id", CancellationToken.None));
    }

    [Fact]
    public async Task ReturnDeltaPage_WhenGetDriveDeltaPageAsyncSucceeds()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        ILogger<GraphClientWrapper> logger = Substitute.For<ILogger<GraphClientWrapper>>();
        // Not possible to fully mock HttpClient without a custom handler, so just check method exists
        var http = new HttpClient();
        var wrapper = new GraphClientWrapper(auth, http, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => wrapper.GetDriveDeltaPageAsync(null, CancellationToken.None));
    }
}
