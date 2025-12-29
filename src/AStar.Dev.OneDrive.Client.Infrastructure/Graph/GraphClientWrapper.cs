using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AStar.Dev.OneDrive.Client.Core.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Graph;

public sealed class GraphClientWrapper(IAuthService auth, HttpClient http, MsalConfigurationSettings msalConfigurationSettings, ILogger<GraphClientWrapper> logger) : IGraphClient
{
    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        var url = GetDeltaOrNextUrl(deltaOrNextLink);

        var token = await auth.GetAccessTokenAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        _ = res.EnsureSuccessStatusCode();

        await using Stream stream = await res.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        List<DriveItemRecord> items = ParseDriveItemRecords(doc);

        var next = TryGetODataProperty(doc, "@odata.nextLink");
        var delta = TryGetODataProperty(doc, "@odata.deltaLink");

        return new DeltaPage(items, next, delta);
    }

    public async Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct)
    {
        try
        {
            logger.LogDebug("Requesting download for DriveItemId: {DriveItemId}", driveItemId);
            var token = await auth.GetAccessTokenAsync(ct);
            var url = $"{msalConfigurationSettings.GraphUri}/items/{driveItemId}/content";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            _ = res.EnsureSuccessStatusCode();

            Stream stream = await res.Content.ReadAsStreamAsync(ct);
            logger.LogDebug("Download stream acquired for DriveItemId: {DriveItemId}", driveItemId);

            return stream;
        }
        catch(HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed for DriveItemId: {DriveItemId}. Status: {StatusCode}, Message: {Message}",
                driveItemId, ex.StatusCode, ex.Message);
        }
        catch(IOException ex)
        {
            logger.LogError(ex, "Network I/O error downloading DriveItemId: {DriveItemId}. This may indicate a connection reset or timeout. Message: {Message}",
                driveItemId, ex.Message);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Unexpected error downloading DriveItemId: {DriveItemId}. Type: {ExceptionType}, Message: {Message}",
                driveItemId, ex.GetType().Name, ex.Message);
        }

        throw new InvalidOperationException("Failed to download DriveItem content");
    }

    public async Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct)
    {
        var token = await auth.GetAccessTokenAsync(ct);

        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var url = $"{msalConfigurationSettings.GraphUri}/root:/{Uri.EscapeDataString(path)}:/createUploadSession";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage res = await http.SendAsync(req, ct);
        _ = res.EnsureSuccessStatusCode();
        await using Stream stream = await res.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var uploadUrl = doc.RootElement.GetProperty("uploadUrl").GetString()!;
        DateTimeOffset expiration = doc.RootElement.TryGetProperty("expirationDateTime", out JsonElement ex) ? DateTimeOffset.Parse(ex.GetString()!, CultureInfo.InvariantCulture) : DateTimeOffset.UtcNow.AddHours(1);

        return new UploadSessionInfo(uploadUrl, Guid.CreateVersion7().ToString(), expiration);
    }

    public async Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, session.UploadUrl);
        req.Content = new StreamContent(chunk);
        req.Content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/*");
        using HttpResponseMessage res = await http.SendAsync(req, ct);

        if(!res.IsSuccessStatusCode)
            _ = res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct)
    {
        var token = await auth.GetAccessTokenAsync(ct);
        var url = $"{msalConfigurationSettings.GraphUri}/items/{driveItemId}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage res = await http.SendAsync(req, ct);
        if(res.StatusCode is not System.Net.HttpStatusCode.NoContent and not System.Net.HttpStatusCode.NotFound)
            _ = res.EnsureSuccessStatusCode();
    }

    private string GetDeltaOrNextUrl(string? deltaOrNextLink)
        => string.IsNullOrEmpty(deltaOrNextLink)
            ? $"{msalConfigurationSettings.GraphUri}/root/delta"
            : deltaOrNextLink;

    private static List<DriveItemRecord> ParseDriveItemRecords(JsonDocument doc)
    {
        var items = new List<DriveItemRecord>();
        if(doc.RootElement.TryGetProperty("value", out JsonElement arr))
        {
            foreach(JsonElement el in arr.EnumerateArray())
            {
                items.Add(ParseDriveItemRecord(el));
            }
        }

        return items;
    }

    private static DriveItemRecord ParseDriveItemRecord(JsonElement jsonElement)
    {
        var id = jsonElement.GetProperty("id").GetString()!;
        var isFolder = jsonElement.TryGetProperty("folder", out _);
        var size = jsonElement.TryGetProperty("size", out JsonElement sProp) ? sProp.GetInt64() : 0L;
        var parentPath = SetParentPath(jsonElement);
        var name = jsonElement.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? id : id;
        var relativePath = GraphPathHelpers.BuildRelativePath(parentPath, name);
        var eTag = jsonElement.TryGetProperty("eTag", out JsonElement et) ? et.GetString() : null;
        var cTag = jsonElement.TryGetProperty("cTag", out JsonElement ctProp) ? ctProp.GetString() : null;
        DateTimeOffset lastModifiedUtc = GetLastModifiedUtc(jsonElement);
        var isDeleted = jsonElement.TryGetProperty("deleted", out _);

        return new DriveItemRecord(id, id, relativePath, eTag, cTag, size, lastModifiedUtc, isFolder, isDeleted);
    }

    private static DateTimeOffset GetLastModifiedUtc(JsonElement jsonElement) => jsonElement.TryGetProperty("lastModifiedDateTime", out JsonElement lm)
                ? DateTimeOffset.Parse(lm.GetString()!, CultureInfo.InvariantCulture)
                : DateTimeOffset.UtcNow;
    private static string SetParentPath(JsonElement jsonElement)
        => jsonElement.TryGetProperty("parentReference", out JsonElement pr) && pr.TryGetProperty("path", out JsonElement p) ? p.GetString() ?? string.Empty : string.Empty;

    private static string? TryGetODataProperty(JsonDocument doc, string propertyName)
        => doc.RootElement.TryGetProperty(propertyName, out JsonElement prop) ? prop.GetString() : null;
}
