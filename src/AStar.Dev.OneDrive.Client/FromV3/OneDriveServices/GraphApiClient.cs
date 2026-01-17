using AStar.Dev.OneDrive.Client.FromV3.Authentication;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Client.FromV3.OneDriveServices;

/// <summary>
/// Wrapper implementation for Microsoft Graph API client.
/// </summary>
/// <remarks>
/// Wraps GraphServiceClient to provide a testable abstraction for OneDrive operations.
/// </remarks>
public sealed class GraphApiClient : IGraphApiClient
{
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
        => await GetDriveItemChildrenAsync(accountId, itemId, 200, cancellationToken);
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphApiClient"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public GraphApiClient(IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);
        _authService = authService;
    }

    private async Task<GraphServiceClient> CreateGraphClientAsync(string accountId, CancellationToken cancellationToken)
    {
        if(string.IsNullOrEmpty(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        // Create a token provider that uses the auth service
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new GraphTokenProvider(_authService, accountId));

        return new GraphServiceClient(authProvider);
    }

    // Token provider implementation for Graph API
    private sealed class GraphTokenProvider : IAccessTokenProvider
    {
        private readonly IAuthService _authService;
        private readonly string _accountId;

        public GraphTokenProvider(IAuthService authService, string accountId)
        {
            _authService = authService;
            _accountId = accountId;
        }

        public AllowedHostsValidator AllowedHostsValidator => new();

        public async Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            var token = await _authService.GetAccessTokenAsync(_accountId, cancellationToken) ?? throw new InvalidOperationException($"Failed to acquire access token for account: {_accountId}");
            return token;
        }
    }

    /// <inheritdoc/>
    public async Task<Drive?> GetMyDriveAsync(string accountId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        return await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveRootAsync(string accountId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        return drive?.Id is null ? null : await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, string itemId, int maxItemsInBatch = 200, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        await DebugLog.EntryAsync("GraphApiClient.GetDriveItemChildrenAsync", cancellationToken);
        await DebugLog.InfoAsync("GraphApiClient.GetDriveItemChildrenAsync", $"Fetching children for item ID: {itemId}", cancellationToken);

        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            await DebugLog.ErrorAsync("GraphApiClient.GetDriveItemChildrenAsync", "Drive ID is null", null, cancellationToken);
            await DebugLog.ExitAsync("GraphApiClient.GetDriveItemChildrenAsync", cancellationToken);
            return [];
        }

        await DebugLog.InfoAsync("GraphApiClient.GetDriveItemChildrenAsync", $"Using drive ID: {drive.Id}", cancellationToken);

        var itemsList = new List<DriveItem>();
        string? nextLink = null;
        do
        {
            DriveItemCollectionResponse? response = nextLink is null
                ? await graphClient.Drives[drive.Id].Items[itemId].Children.GetAsync(q => q.QueryParameters.Top = maxItemsInBatch, cancellationToken: cancellationToken)
                : await graphClient.RequestAdapter.SendAsync<Microsoft.Graph.Models.DriveItemCollectionResponse>(
                    new Microsoft.Kiota.Abstractions.RequestInformation
                    {
                        HttpMethod = Microsoft.Kiota.Abstractions.Method.GET,
                        URI = new Uri(nextLink)
                    },
                    DriveItemCollectionResponse.CreateFromDiscriminatorValue,
                    null,
                    cancellationToken);

            await DebugLog.InfoAsync("GraphApiClient.GetDriveItemChildrenAsync", $"Response received - Value count: {response?.Value?.Count ?? 0}, NextLink: {response?.OdataNextLink}", cancellationToken);

            IEnumerable<DriveItem> items = response?.Value?.Where(item => item.Deleted is null) ?? [];
            itemsList.AddRange(items);

            nextLink = response?.OdataNextLink;
        } while(!string.IsNullOrEmpty(nextLink));

        await DebugLog.InfoAsync("GraphApiClient.GetDriveItemChildrenAsync", $"After filtering deleted items: {itemsList.Count} items", cancellationToken);
        await DebugLog.ExitAsync("GraphApiClient.GetDriveItemChildrenAsync", cancellationToken);
        return itemsList;
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveItemAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        return drive?.Id is null ? null : await graphClient.Drives[drive.Id].Items[itemId].GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetRootChildrenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            return [];
        }

        DriveItem? root = await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
        if(root?.Id is null)
        {
            return [];
        }

        DriveItemCollectionResponse? response = await graphClient.Drives[drive.Id].Items[root.Id].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }

    /// <inheritdoc/>
    public async Task DownloadFileAsync(string accountId, string itemId, string localFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(localFilePath);

        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            throw new InvalidOperationException("Unable to access user's drive");
        }

        // Download file content stream from OneDrive
        Stream contentStream = await graphClient.Drives[drive.Id].Items[itemId].Content.GetAsync(cancellationToken: cancellationToken) ?? throw new InvalidOperationException($"Failed to download file content for item {itemId}");

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(localFilePath);
        if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        // Write the stream to local file
        using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await contentStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveItem> UploadFileAsync(string accountId, string localFilePath, string remotePath, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localFilePath);
        ArgumentNullException.ThrowIfNull(remotePath);

        if(!File.Exists(localFilePath))
        {
            throw new FileNotFoundException($"Local file not found: {localFilePath}", localFilePath);
        }

        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            throw new InvalidOperationException("Unable to access user's drive");
        }

        var fileInfo = new FileInfo(localFilePath);
        const long SmallFileThreshold = 4 * 1024 * 1024; // 4MB - Graph API threshold for simple upload

        // Normalize remote path (remove leading slash, Graph API uses root:/{path} format)
        var normalizedPath = remotePath.TrimStart('/');

        if(fileInfo.Length < SmallFileThreshold)
        {
            // Simple upload for small files - use path-based addressing
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // PUT to /drives/{driveId}/root:/{path}:/content
            DriveItem uploadedItem = await graphClient.Drives[drive.Id]
                .Items[$"root:/{normalizedPath}:"]
                .Content
                .PutAsync(fileStream, cancellationToken: cancellationToken) ?? throw new InvalidOperationException($"Upload failed for file: {localFilePath}");

            // Report full file size as uploaded for small files
            progress?.Report(fileInfo.Length);

            return uploadedItem;
        }
        else
        {
            // Resumable upload session for large files
            var requestBody = new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "replace" }
                    }
                }
            };

            UploadSession? uploadSession = await graphClient.Drives[drive.Id]
                .Items[$"root:/{normalizedPath}:"]
                .CreateUploadSession
                .PostAsync(requestBody, cancellationToken: cancellationToken);

            if(uploadSession?.UploadUrl is null)
            {
                throw new InvalidOperationException($"Failed to create upload session for file: {localFilePath}");
            }

            // Upload in chunks (recommended 5-10MB per chunk for optimal performance)
            const int ChunkSize = 5 * 1024 * 1024; // 5MB chunks
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            long position = 0;
            var totalLength = fileStream.Length;

            using var httpClient = new HttpClient();

            while(position < totalLength)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunkSize = (int)Math.Min(ChunkSize, totalLength - position);
                var chunk = new byte[chunkSize];
                var bytesRead = await fileStream.ReadAsync(chunk.AsMemory(0, chunkSize), cancellationToken);

                if(bytesRead != chunkSize)
                {
                    throw new InvalidOperationException($"Failed to read expected bytes from file. Expected: {chunkSize}, Read: {bytesRead}");
                }

                var contentRange = $"bytes {position}-{position + chunkSize - 1}/{totalLength}";

                using var content = new ByteArrayContent(chunk);
                content.Headers.ContentLength = chunkSize;
                content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(position, position + chunkSize - 1, totalLength);

                HttpResponseMessage response = await httpClient.PutAsync(uploadSession.UploadUrl, content, cancellationToken);

                if(!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    throw new InvalidOperationException($"Chunk upload failed. Status: {response.StatusCode}, Range: {contentRange}");
                }

                position += chunkSize;

                // Report progress after each chunk
                progress?.Report(position);

                // If this is the last chunk, parse the response to get the DriveItem
                if(position >= totalLength && response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    DriveItem uploadedItem = System.Text.Json.JsonSerializer.Deserialize<DriveItem>(responseContent) ?? throw new InvalidOperationException("Failed to deserialize uploaded DriveItem from response");
                    return uploadedItem;
                }
            }

            throw new InvalidOperationException("Upload completed but no DriveItem was returned");
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(itemId);

        await DebugLog.EntryAsync("GraphApiClient.DeleteFileAsync", cancellationToken);
        await DebugLog.InfoAsync("GraphApiClient.DeleteFileAsync", $"Attempting to delete remote file. AccountId: {accountId}, ItemId: {itemId}", cancellationToken);

        GraphServiceClient graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            await DebugLog.ErrorAsync("GraphApiClient.DeleteFileAsync", "Drive ID is null. Cannot delete file.", null, cancellationToken);
            await DebugLog.ExitAsync("GraphApiClient.DeleteFileAsync", cancellationToken);
            throw new InvalidOperationException("Unable to access user's drive");
        }

        // DELETE /drives/{driveId}/items/{itemId}
        try
        {
            await graphClient.Drives[drive.Id].Items[itemId].DeleteAsync(cancellationToken: cancellationToken);
            await DebugLog.InfoAsync("GraphApiClient.DeleteFileAsync", $"Successfully deleted remote file. AccountId: {accountId}, ItemId: {itemId}", cancellationToken);
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync("GraphApiClient.DeleteFileAsync", $"Exception during remote file deletion. AccountId: {accountId}, ItemId: {itemId}", ex, cancellationToken);
            throw;
        }
        finally
        {
            await DebugLog.ExitAsync("GraphApiClient.DeleteFileAsync", cancellationToken);
        }
    }
}
