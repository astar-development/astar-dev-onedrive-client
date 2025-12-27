using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Graph;

public sealed class GraphClientWrapper(IAuthService auth, HttpClient http, ILogger<GraphClientWrapper> logger) : IGraphClient
{
    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
#pragma warning disable S1075 // URIs should not be hardcoded
        var url = GetDeltaOrNextUrl(deltaOrNextLink);
#pragma warning restore S1075 // URIs should not be hardcoded

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
            var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}/content";
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
        // Build path under root
        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var url = $"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(path)}:/createUploadSession";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage res = await http.SendAsync(req, ct);
        _ = res.EnsureSuccessStatusCode();
        await using Stream stream = await res.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var uploadUrl = doc.RootElement.GetProperty("uploadUrl").GetString()!;
        DateTimeOffset expiration = doc.RootElement.TryGetProperty("expirationDateTime", out JsonElement ex) ? DateTimeOffset.Parse(ex.GetString()!, CultureInfo.InvariantCulture) : DateTimeOffset.UtcNow.AddHours(1);
        return new UploadSessionInfo(uploadUrl, Guid.NewGuid().ToString(), expiration);
    }

    public async Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, session.UploadUrl);
        req.Content = new StreamContent(chunk);
        req.Content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/*");
        using HttpResponseMessage res = await http.SendAsync(req, ct);
        // Graph returns 201/200 when upload completes, 202 for accepted chunk
        if(!res.IsSuccessStatusCode)
            _ = res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct)
    {
        var token = await auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage res = await http.SendAsync(req, ct);
        if(res.StatusCode is not System.Net.HttpStatusCode.NoContent and not System.Net.HttpStatusCode.NotFound)
            _ = res.EnsureSuccessStatusCode();
    }

    private static string GetDeltaOrNextUrl(string? deltaOrNextLink)
        => string.IsNullOrEmpty(deltaOrNextLink)
            ? "https://graph.microsoft.com/v1.0/me/drive/root/delta"
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

    private static DriveItemRecord ParseDriveItemRecord(JsonElement el)
    {
        var id = el.GetProperty("id").GetString()!;
        var isFolder = el.TryGetProperty("folder", out _);
        var size = el.TryGetProperty("size", out JsonElement sProp) ? sProp.GetInt64() : 0L;
        var parentPath = el.TryGetProperty("parentReference", out JsonElement pr) && pr.TryGetProperty("path", out JsonElement p) ? p.GetString() ?? string.Empty : string.Empty;
        var name = el.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? id : id;
        var relativePath = GraphPathHelpers.BuildRelativePath(parentPath, name);
        var eTag = el.TryGetProperty("eTag", out JsonElement et) ? et.GetString() : null;
        var cTag = el.TryGetProperty("cTag", out JsonElement ctProp) ? ctProp.GetString() : null;
        DateTimeOffset last = el.TryGetProperty("lastModifiedDateTime", out JsonElement lm)
            ? DateTimeOffset.Parse(lm.GetString()!, CultureInfo.InvariantCulture)
            : DateTimeOffset.UtcNow;
        var isDeleted = el.TryGetProperty("deleted", out _);
        return new DriveItemRecord(id, id, relativePath, eTag, cTag, size, last, isFolder, isDeleted);
    }

    private static string? TryGetODataProperty(JsonDocument doc, string propertyName)
        => doc.RootElement.TryGetProperty(propertyName, out JsonElement prop) ? prop.GetString() : null;
}
