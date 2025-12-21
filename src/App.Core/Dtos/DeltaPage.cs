using App.Core.Entities;

namespace App.Core.Dtos;

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);
