using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace App.Infrastructure.Data.Converters;

public static class SqliteTypeConverters
{
    public static ValueConverter<DateTimeOffset, long> DateTimeOffsetToTicks { get; } =
        new(dto => dto.ToUniversalTime().UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));

    public static ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetToTicks { get; } =
        new(dto => dto.HasValue ? dto.Value.ToUniversalTime().UtcTicks : (long?)null,
            ticks => ticks.HasValue ? new DateTimeOffset(ticks.Value, TimeSpan.Zero) : (DateTimeOffset?)null);

    public static ValueConverter<TimeSpan, long> TimeSpanToTicks { get; } =
        new(ts => ts.Ticks, ticks => TimeSpan.FromTicks(ticks));

    public static ValueConverter<TimeSpan?, long?> NullableTimeSpanToTicks { get; } =
        new(ts => ts.HasValue ? ts.Value.Ticks : (long?)null, ticks => ticks.HasValue ? TimeSpan.FromTicks(ticks.Value) : (TimeSpan?)null);

    public static ValueConverter<Guid, byte[]> GuidToBytes { get; } =
        new(g => g.ToByteArray(), b => new Guid(b));

    // Allow nullable byte[] result type to avoid nullability warning
    public static ValueConverter<Guid?, byte[]?> NullableGuidToBytes { get; } =
        new(g => g.HasValue ? g.Value.ToByteArray() : null, b => b != null ? new Guid(b) : (Guid?)null);

    public static ValueConverter<decimal, long> DecimalToCents { get; } =
        new(d => (long)Math.Round(d * 100m), l => l / 100m);

    public static ValueConverter<decimal?, long?> NullableDecimalToCents { get; } =
        new(d => d.HasValue ? (long?)Math.Round(d.Value * 100m) : null, l => l.HasValue ? l.Value / 100m : (decimal?)null);
}
