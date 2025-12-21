using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Core.Dtos;

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);
