
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Infrastructure.Graph;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit;

public class GraphClientWrapperShould
{
    private static (
        GraphClientWrapper sut,
        IAuthService auth,
        TestHandler handler,
        ILogger<GraphClientWrapper> logger
    ) CreateSut()
    {
        IAuthService auth = Substitute.For<IAuthService>();
        auth.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token");
        var handler = new TestHandler();
        var http = new HttpClient(handler);
        ILogger<GraphClientWrapper> logger = Substitute.For<ILogger<GraphClientWrapper>>();
        var sut = new GraphClientWrapper(auth, http, logger);
        return (sut, auth, handler, logger);
    }

    [Fact]
    public async Task GetDriveDeltaPageAsync_ReturnsDeltaPage()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        var json = """
        {
          "value": [
            {
              "id": "id1",
              "name": "file.txt",
              "size": 123,
              "lastModifiedDateTime": "2024-01-01T12:00:00Z"
            }
          ],
          "@odata.nextLink": "next",
          "@odata.deltaLink": "delta"
        }
        """;
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        DeltaPage result = await sut.GetDriveDeltaPageAsync(null, CancellationToken.None);
        result.Items.ShouldNotBeEmpty();
        result.NextLink.ShouldBe("next");
        result.DeltaLink.ShouldBe("delta");
    }

    [Fact]
    public async Task GetDriveDeltaPageAsync_HandlesEmptyValue()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        var json = "{ \"value\": [], \"@odata.deltaLink\": \"delta\" }";
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        DeltaPage result = await sut.GetDriveDeltaPageAsync(null, CancellationToken.None);
        result.Items.ShouldBeEmpty();
        result.DeltaLink.ShouldBe("delta");
    }

    [Fact]
    public async Task GetDriveDeltaPageAsync_ThrowsOnMalformedJson()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json")
        };
        await Should.ThrowAsync<JsonException>(() => sut.GetDriveDeltaPageAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadDriveItemContentAsync_ReturnsStreamOnSuccess()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        var content = new StringContent("abc");
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        Stream stream = await sut.DownloadDriveItemContentAsync("id1", CancellationToken.None);
        stream.ShouldNotBeNull();
    }

    [Fact]
    public async Task DownloadDriveItemContentAsync_ThrowsOnHttpError()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.NotFound);
        await Should.ThrowAsync<InvalidOperationException>(() => sut.DownloadDriveItemContentAsync("badid", CancellationToken.None));
    }

    [Fact]
    public async Task CreateUploadSessionAsync_ReturnsSessionInfo()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        var json = "{ \"uploadUrl\": \"http://upload\", \"expirationDateTime\": \"2024-12-31T23:59:59Z\" }";
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        UploadSessionInfo result = await sut.CreateUploadSessionAsync("", "file.txt", CancellationToken.None);
        result.UploadUrl.ShouldBe("http://upload");
        result.ExpiresAt.ShouldBe(DateTimeOffset.Parse("2024-12-31T23:59:59Z", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task UploadChunkAsync_SucceedsOn200()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.OK);
        var session = new UploadSessionInfo("http://upload", "id", DateTimeOffset.UtcNow.AddMinutes(5));
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        await Should.NotThrowAsync(() => sut.UploadChunkAsync(session, ms, 0, 2, CancellationToken.None));
    }

    [Fact]
    public async Task UploadChunkAsync_ThrowsOnError()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var session = new UploadSessionInfo("http://upload", "id", DateTimeOffset.UtcNow.AddMinutes(5));
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("abc"));
        await Should.ThrowAsync<HttpRequestException>(() => sut.UploadChunkAsync(session, ms, 0, 2, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteDriveItemAsync_SucceedsOnNoContent()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.NoContent);
        await Should.NotThrowAsync(() => sut.DeleteDriveItemAsync("id", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteDriveItemAsync_SucceedsOnNotFound()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.NotFound);
        await Should.NotThrowAsync(() => sut.DeleteDriveItemAsync("id", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteDriveItemAsync_ThrowsOnOtherStatus()
    {
        (GraphClientWrapper? sut, IAuthService _, TestHandler? handler, ILogger<GraphClientWrapper> _) = CreateSut();
        handler.Response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        await Should.ThrowAsync<HttpRequestException>(() => sut.DeleteDriveItemAsync("id", CancellationToken.None));
    }

    private class TestHandler : HttpMessageHandler
    {
        public HttpResponseMessage? Response { get; set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Response switch
            {
                null => throw new InvalidOperationException("No response set"),
                _ => Task.FromResult(Response)
            };
    }
}
