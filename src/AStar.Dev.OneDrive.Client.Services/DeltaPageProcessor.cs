using AStar.Dev.OneDrive.Client.Core.Dtos;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc/>
public class DeltaPageProcessor(ISyncRepository repo, IGraphClient graph, ILogger<DeltaPageProcessor> logger) : IDeltaPageProcessor
{

    /// <inheritdoc/>
    public async Task<(string? finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[DeltaPageProcessor] Starting delta page processing");
        string? nextOrDelta = null, finalDelta = null;
        int pageCount = 0, totalItemsProcessed = 0;
        try
        {
            do
            {
                logger.LogDebug("[DeltaPageProcessor] Requesting delta page: nextOrDelta={NextOrDelta}", nextOrDelta);
                DeltaPage page = await graph.GetDriveDeltaPageAsync(nextOrDelta, cancellationToken);
                logger.LogDebug("[DeltaPageProcessor] Received page: items={Count} nextLink={NextLink} deltaLink={DeltaLink}", page.Items.Count(), page.NextLink, page.DeltaLink);
                await repo.ApplyDriveItemsAsync(page.Items, cancellationToken);
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
            throw;
        }

        return (finalDelta, pageCount, totalItemsProcessed);
    }

    /// <inheritdoc/>
    public async Task<(string? finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(
        CancellationToken cancellationToken,
        Action<SyncProgress>? progressCallback)
    {
        logger.LogInformation("[DeltaPageProcessor] Starting delta page processing (with progress callback)");
        string? nextOrDelta = null, finalDelta = null;
        int pageCount = 0, totalItemsProcessed = 0;
        try
        {
            do
            {
                logger.LogDebug("[DeltaPageProcessor] Requesting delta page: nextOrDelta={NextOrDelta}", nextOrDelta);
                DeltaPage page = await graph.GetDriveDeltaPageAsync(nextOrDelta, cancellationToken);
                logger.LogDebug("[DeltaPageProcessor] Received page: items={Count} nextLink={NextLink} deltaLink={DeltaLink}", page.Items.Count(), page.NextLink, page.DeltaLink);
                await repo.ApplyDriveItemsAsync(page.Items, cancellationToken);
                totalItemsProcessed += page.Items.Count();
                nextOrDelta = page.NextLink;
                finalDelta = page.DeltaLink ?? finalDelta;
                pageCount++;
                logger.LogInformation("[DeltaPageProcessor] Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                    pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
                progressCallback?.Invoke(new SyncProgress
                {
                    OperationType = SyncOperationType.Syncing,
                    ProcessedFiles = totalItemsProcessed,
                    TotalFiles = 0, // Unknown at this stage
                    PendingDownloads = 0,
                    PendingUploads = 0,
                    CurrentOperationMessage = $"Delta page {pageCount} applied ({page.Items.Count()} items)",
                    Timestamp = DateTimeOffset.Now
                });
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
            progressCallback?.Invoke(new SyncProgress
            {
                OperationType = SyncOperationType.Failed,
                ProcessedFiles = totalItemsProcessed,
                TotalFiles = 0,
                PendingDownloads = 0,
                PendingUploads = 0,
                CurrentOperationMessage = $"Delta sync failed: {ex.Message}",
                Timestamp = DateTimeOffset.Now
            });
            throw;
        }

        return (finalDelta, pageCount, totalItemsProcessed);
    }
}
