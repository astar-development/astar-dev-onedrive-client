using System.Net.Http.Headers;
using System.Text.Json;
using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Graph;

public sealed class GraphClientWrapper : IGraphClient
{
    private readonly IAuthService _auth;
    private readonly HttpClient _http;
    private readonly ILogger<GraphClientWrapper> _logger;

    public GraphClientWrapper(IAuthService auth, HttpClient http, ILogger<GraphClientWrapper> logger)
    {
        _auth = auth;
        _http = http;
        _logger = logger;
    }

    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        var url = string.IsNullOrEmpty(deltaOrNextLink)
            ? "https://graph.microsoft.com/v1.0/me/drive/root/delta"
            : deltaOrNextLink;

        var token = await _auth.GetAccessTokenAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        _ = res.EnsureSuccessStatusCode();

        await using Stream stream = await res.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var items = new List<DriveItemRecord>();
        if(doc.RootElement.TryGetProperty("value", out JsonElement arr))
        {
            foreach(JsonElement el in arr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString()!;
                var isFolder = el.TryGetProperty("folder", out _);
                var size = el.TryGetProperty("size", out JsonElement sProp) ? sProp.GetInt64() : 0L;
                var parentPath = el.TryGetProperty("parentReference", out JsonElement pr) && pr.TryGetProperty("path", out JsonElement p) ? p.GetString() ?? string.Empty : string.Empty;
                var name = el.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? id : id;
                var relativePath = GraphPathHelpers.BuildRelativePath(parentPath, name);
                var eTag = el.TryGetProperty("eTag", out JsonElement et) ? et.GetString() : null;
                var cTag = el.TryGetProperty("cTag", out JsonElement ctProp) ? ctProp.GetString() : null;
                DateTimeOffset last = el.TryGetProperty("lastModifiedDateTime", out JsonElement lm) ? DateTimeOffset.Parse(lm.GetString()!) : DateTimeOffset.UtcNow;
                var isDeleted = el.TryGetProperty("deleted", out _);
                items.Add(new DriveItemRecord(id, id, relativePath, eTag, cTag, size, last, isFolder, isDeleted));
            }
        }

            var next = doc.RootElement.TryGetProperty("@odata.nextLink", out JsonElement nl) ? nl.GetString() : null;
            var delta = doc.RootElement.TryGetProperty("@odata.deltaLink", out JsonElement dl) ? dl.GetString() : null;
            return new DeltaPage(items, next, delta);
        }

        public async Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct)
        {
            try
            {
                _logger.LogDebug("Requesting download for DriveItemId: {DriveItemId}", driveItemId);
                var token = await _auth.GetAccessTokenAsync(ct);
                var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}/content";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                _ = res.EnsureSuccessStatusCode();

            Stream stream = await res.Content.ReadAsStreamAsync(ct);
                _logger.LogDebug("Download stream acquired for DriveItemId: {DriveItemId}", driveItemId);
                return stream;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for DriveItemId: {DriveItemId}. Status: {StatusCode}, Message: {Message}", 
                    driveItemId, ex.StatusCode, ex.Message);
                throw;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Network I/O error downloading DriveItemId: {DriveItemId}. This may indicate a connection reset or timeout. Message: {Message}", 
                    driveItemId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading DriveItemId: {DriveItemId}. Type: {ExceptionType}, Message: {Message}", 
                    driveItemId, ex.GetType().Name, ex.Message);
                throw;
            }
        }

    public async Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        // Build path under root
        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var url = $"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(path)}:/createUploadSession";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage res = await _http.SendAsync(req, ct);
        _ = res.EnsureSuccessStatusCode();
        await using Stream stream = await res.Content.ReadAsStreamAsync(ct);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var uploadUrl = doc.RootElement.GetProperty("uploadUrl").GetString()!;
        DateTimeOffset expiration = doc.RootElement.TryGetProperty("expirationDateTime", out JsonElement ex) ? DateTimeOffset.Parse(ex.GetString()!) : DateTimeOffset.UtcNow.AddHours(1);
        return new UploadSessionInfo(uploadUrl, Guid.NewGuid().ToString(), expiration);
    }

    public async Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, session.UploadUrl);
        req.Content = new StreamContent(chunk);
        req.Content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/*");
        using HttpResponseMessage res = await _http.SendAsync(req, ct);
        // Graph returns 201/200 when upload completes, 202 for accepted chunk
        if(!res.IsSuccessStatusCode)
            _ = res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage res = await _http.SendAsync(req, ct);
        if(res.StatusCode is not System.Net.HttpStatusCode.NoContent and not System.Net.HttpStatusCode.NotFound)
            _ = res.EnsureSuccessStatusCode();
    }
}
