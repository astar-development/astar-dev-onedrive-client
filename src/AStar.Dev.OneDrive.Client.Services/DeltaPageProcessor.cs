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
        string? nextOrDelta = null, finalDelta = null;
        int pageCount = 0, totalItemsProcessed = 0;
        do
        {
            DeltaPage page = await graph.GetDriveDeltaPageAsync(nextOrDelta, cancellationToken);
            await repo.ApplyDriveItemsAsync(page.Items, cancellationToken);
            totalItemsProcessed += page.Items.Count();
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            pageCount++;
            logger.LogInformation("Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
        } while(!string.IsNullOrEmpty(nextOrDelta) && !cancellationToken.IsCancellationRequested);
        return (finalDelta, pageCount, totalItemsProcessed);
    }
}
