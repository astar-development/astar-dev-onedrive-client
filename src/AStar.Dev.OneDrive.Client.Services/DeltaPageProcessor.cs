using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class DeltaPageProcessor(ISyncRepository repo, IGraphClient graph, ILogger<DeltaPageProcessor> logger) : IDeltaPageProcessor
{

    /// <inheritdoc/>
    public async Task<(string? finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(string accountId, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DeltaPageProcessor] Starting delta page processing");
        string? nextOrDelta = null, finalDelta = null;
        int pageCount = 0, totalItemsProcessed = 0;
        try
        {
            do
            {
                logger.LogDebug("[DeltaPageProcessor] Requesting delta page: nextOrDelta={NextOrDelta}", nextOrDelta);
                DeltaPage page = await graph.GetDriveDeltaPageAsync(accountId, nextOrDelta, cancellationToken);
                logger.LogDebug("[DeltaPageProcessor] Received page: items={Count} nextLink={NextLink} deltaLink={DeltaLink}", page.Items.Count(), page.NextLink, page.DeltaLink);
                await repo.ApplyDriveItemsAsync(accountId, page.Items, cancellationToken);
                totalItemsProcessed += page.Items.Count();
                nextOrDelta = page.NextLink;
                finalDelta = page.DeltaLink ?? finalDelta;
                pageCount++;
                logger.LogInformation("[DeltaPageProcessor] Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                    pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
                if(pageCount > 10000)
                {
                    logger.LogWarning("[DeltaPageProcessor] Exceeded max page count (10000), aborting to prevent infinite loop.");
                    break;
                }
            } while(!string.IsNullOrEmpty(nextOrDelta) && !cancellationToken.IsCancellationRequested);
            logger.LogInformation("[DeltaPageProcessor] Delta processing complete: finalDelta={FinalDelta} pageCount={PageCount} totalItems={TotalItems}", finalDelta, pageCount, totalItemsProcessed);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "[DeltaPageProcessor] Exception during delta processing: {Message}", ex.Message);
            throw new IOException("Error processing delta pages", ex);
        }

        return (finalDelta, pageCount, totalItemsProcessed);
    }

    /// <inheritdoc/>
    public async Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(string accountId, DeltaToken deltaToken, CancellationToken cancellationToken, Action<SyncProgress>? progressCallback)
    {
        logger.LogInformation("[DeltaPageProcessor] Starting delta page processing (with progress callback)");
        string? nextOrDelta = null;
        DeltaToken finalToken = deltaToken;
        int pageCount = 0, totalItemsProcessed = 0;
        try
        {
            do
            {
                logger.LogDebug("[DeltaPageProcessor] Requesting delta page: nextOrDelta={NextOrDelta}", nextOrDelta);
                DeltaPage page = await graph.GetDriveDeltaPageAsync(accountId, nextOrDelta, cancellationToken);
                DriveItemRecord? driveItemRecord = page.Items.FirstOrDefault();
                if(driveItemRecord is not null)
                {
                    deltaToken = new DeltaToken("PlaceholderAccountId", driveItemRecord.Id.Split('!')[0], page.DeltaLink ?? string.Empty, DateTimeOffset.UtcNow);
                }

                logger.LogDebug("[DeltaPageProcessor] Received page: items={Count} nextLink={NextLink} deltaLink={DeltaLink}", page.Items.Count(), page.NextLink, page.DeltaLink);
                await repo.ApplyDriveItemsAsync(accountId, page.Items, cancellationToken);
                totalItemsProcessed += page.Items.Count();
                nextOrDelta = page.NextLink;
                if(page.DeltaLink is not null)
                {
                    finalToken = new("PlaceholderAccountId", deltaToken.Id, page.DeltaLink, DateTimeOffset.UtcNow);
                }

                pageCount++;
                logger.LogInformation("[DeltaPageProcessor] Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                    pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
                progressCallback?.Invoke(CreateSyncProgressMessage(pageCount, totalItemsProcessed, page));
                if(pageCount > 10000)
                {
                    logger.LogWarning("[DeltaPageProcessor] Exceeded max page count (10000), aborting to prevent infinite loop.");
                    break;
                }
            } while(!string.IsNullOrEmpty(nextOrDelta) && !cancellationToken.IsCancellationRequested);
            logger.LogInformation("[DeltaPageProcessor] Delta processing complete: finalToken={FinalToken} pageCount={PageCount} totalItems={TotalItems}", finalToken, pageCount, totalItemsProcessed);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "[DeltaPageProcessor] Exception during delta processing: {Message}", ex.Message);
            progressCallback?.Invoke(CreateErrorSyncProgress(totalItemsProcessed, ex.GetBaseException()?.Message ?? "Unknown error"));
            throw new IOException("Error processing delta pages", ex);
        }

        return (finalToken, pageCount, totalItemsProcessed);
    }

    private static SyncProgress CreateSyncProgressMessage(int pageCount, int totalItemsProcessed, DeltaPage page) => new()
    {
        OperationType = SyncOperationType.Syncing,
        ProcessedFiles = totalItemsProcessed,
        TotalFiles = 0, // Unknown at this stage
        PendingDownloads = 0,
        PendingUploads = 0,
        CurrentOperationMessage = $"Delta page {pageCount} applied ({page.Items.Count()} items)",
        Timestamp = DateTimeOffset.Now
    };

    private static SyncProgress CreateErrorSyncProgress(int totalItemsProcessed, string errorMessage) => new()
    {
        OperationType = SyncOperationType.Failed,
        ProcessedFiles = totalItemsProcessed,
        TotalFiles = 0,
        PendingDownloads = 0,
        PendingUploads = 0,
        CurrentOperationMessage = $"Delta sync failed: {errorMessage}",
        Timestamp = DateTimeOffset.Now
    };
}
