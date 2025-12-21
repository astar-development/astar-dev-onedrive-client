using App.Core.Entities;

namespace App.Core.Dto;

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);
