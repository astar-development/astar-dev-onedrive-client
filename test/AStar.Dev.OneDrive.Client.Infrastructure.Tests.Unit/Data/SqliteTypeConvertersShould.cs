using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Data;

public class SqliteTypeConvertersShould
{
    [Fact]
    public void ConvertDateTimeOffsetToTicksAndBack()
    {
        DateTimeOffset original = new(2024, 12, 23, 15, 30, 45, TimeSpan.FromHours(5));

        var ticks = (long)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertToProvider(original)!;
        var roundTrip = (DateTimeOffset)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ToUniversalTime().ShouldBe(original.ToUniversalTime());
    }

    [Fact]
    public void ConvertDateTimeOffsetToUtcBeforeStoringTicks()
    {
        DateTimeOffset withOffset = new(2024, 12, 23, 15, 30, 45, TimeSpan.FromHours(5));
        DateTimeOffset utc = withOffset.ToUniversalTime();

        var ticks = (long)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertToProvider(withOffset)!;

        ticks.ShouldBe(utc.UtcTicks);
    }

    [Fact]
    public void RestoreDateTimeOffsetWithZeroOffset()
    {
        var ticks = DateTimeOffset.UtcNow.UtcTicks;

        var result = (DateTimeOffset)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertFromProvider(ticks)!;

        result.Offset.ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(2024, 1, 1, 0, 0, 0)]
    [InlineData(2024, 12, 31, 23, 59, 59)]
    [InlineData(1970, 1, 1, 0, 0, 0)]
    [InlineData(2100, 6, 15, 12, 30, 45)]
    public void ConvertVariousDateTimeOffsetsToTicksAndBack(int year, int month, int day, int hour, int minute, int second)
    {
        DateTimeOffset original = new(year, month, day, hour, minute, second, TimeSpan.Zero);

        var ticks = (long)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertToProvider(original)!;
        var roundTrip = (DateTimeOffset)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullableDateTimeOffsetWithValueToTicksAndBack()
    {
        DateTimeOffset? original = new DateTimeOffset(2024, 12, 23, 15, 30, 45, TimeSpan.Zero);

        var ticks = (long?)SqliteTypeConverters.NullableDateTimeOffsetToTicks.ConvertToProvider(original);
        var roundTrip = (DateTimeOffset?)SqliteTypeConverters.NullableDateTimeOffsetToTicks.ConvertFromProvider(ticks);

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullDateTimeOffsetToNullTicks()
    {
        DateTimeOffset? original = null;

        var ticks = (long?)SqliteTypeConverters.NullableDateTimeOffsetToTicks.ConvertToProvider(original);

        ticks.ShouldBeNull();
    }

    [Fact]
    public void ConvertNullTicksToNullDateTimeOffset()
    {
        long? ticks = null;

        var result = (DateTimeOffset?)SqliteTypeConverters.NullableDateTimeOffsetToTicks.ConvertFromProvider(ticks);

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertTimeSpanToTicksAndBack()
    {
        TimeSpan original = TimeSpan.FromHours(3) + TimeSpan.FromMinutes(45) + TimeSpan.FromSeconds(30);

        var ticks = (long)SqliteTypeConverters.TimeSpanToTicks.ConvertToProvider(original)!;
        var roundTrip = (TimeSpan)SqliteTypeConverters.TimeSpanToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ShouldBe(original);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3600)]
    [InlineData(86400)]
    [InlineData(-3600)]
    public void ConvertVariousTimeSpansInSecondsToTicksAndBack(int seconds)
    {
        var original = TimeSpan.FromSeconds(seconds);

        var ticks = (long)SqliteTypeConverters.TimeSpanToTicks.ConvertToProvider(original)!;
        var roundTrip = (TimeSpan)SqliteTypeConverters.TimeSpanToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertTimeSpanMaxValueToTicksAndBack()
    {
        TimeSpan original = TimeSpan.MaxValue;

        var ticks = (long)SqliteTypeConverters.TimeSpanToTicks.ConvertToProvider(original)!;
        var roundTrip = (TimeSpan)SqliteTypeConverters.TimeSpanToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertTimeSpanMinValueToTicksAndBack()
    {
        TimeSpan original = TimeSpan.MinValue;

        var ticks = (long)SqliteTypeConverters.TimeSpanToTicks.ConvertToProvider(original)!;
        var roundTrip = (TimeSpan)SqliteTypeConverters.TimeSpanToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullableTimeSpanWithValueToTicksAndBack()
    {
        TimeSpan? original = TimeSpan.FromMinutes(45);

        var ticks = (long?)SqliteTypeConverters.NullableTimeSpanToTicks.ConvertToProvider(original);
        var roundTrip = (TimeSpan?)SqliteTypeConverters.NullableTimeSpanToTicks.ConvertFromProvider(ticks);

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullTimeSpanToNullTicks()
    {
        TimeSpan? original = null;

        var ticks = (long?)SqliteTypeConverters.NullableTimeSpanToTicks.ConvertToProvider(original);

        ticks.ShouldBeNull();
    }

    [Fact]
    public void ConvertNullTicksToNullTimeSpan()
    {
        long? ticks = null;

        var result = (TimeSpan?)SqliteTypeConverters.NullableTimeSpanToTicks.ConvertFromProvider(ticks);

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertGuidToBytesAndBack()
    {
        var original = Guid.NewGuid();

        var bytes = (byte[])SqliteTypeConverters.GuidToBytes.ConvertToProvider(original)!;
        var roundTrip = (Guid)SqliteTypeConverters.GuidToBytes.ConvertFromProvider(bytes)!;

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertGuidToBytesProduces16Bytes()
    {
        var original = Guid.NewGuid();

        var bytes = (byte[])SqliteTypeConverters.GuidToBytes.ConvertToProvider(original)!;

        bytes.Length.ShouldBe(16);
    }

    [Fact]
    public void ConvertEmptyGuidToBytesAndBack()
    {
        Guid original = Guid.Empty;

        var bytes = (byte[])SqliteTypeConverters.GuidToBytes.ConvertToProvider(original)!;
        var roundTrip = (Guid)SqliteTypeConverters.GuidToBytes.ConvertFromProvider(bytes)!;

        roundTrip.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void ConvertSpecificGuidToBytesAndBack()
    {
        Guid original = new("12345678-1234-1234-1234-123456789012");

        var bytes = (byte[])SqliteTypeConverters.GuidToBytes.ConvertToProvider(original)!;
        var roundTrip = (Guid)SqliteTypeConverters.GuidToBytes.ConvertFromProvider(bytes)!;

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullableGuidWithValueToBytesAndBack()
    {
        Guid? original = Guid.NewGuid();

        var bytes = (byte[]?)SqliteTypeConverters.NullableGuidToBytes.ConvertToProvider(original);
        var roundTrip = (Guid?)SqliteTypeConverters.NullableGuidToBytes.ConvertFromProvider(bytes);

        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullGuidToNullBytes()
    {
        Guid? original = null;

        var bytes = (byte[]?)SqliteTypeConverters.NullableGuidToBytes.ConvertToProvider(original);

        bytes.ShouldBeNull();
    }

    [Fact]
    public void ConvertNullBytesToNullGuid()
    {
        byte[]? bytes = null;

        var result = (Guid?)SqliteTypeConverters.NullableGuidToBytes.ConvertFromProvider(bytes);

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertDecimalToCentsAndBack()
    {
        var original = 123.45m;

        var cents = (long)SqliteTypeConverters.DecimalToCents.ConvertToProvider(original)!;
        var roundTrip = (decimal)SqliteTypeConverters.DecimalToCents.ConvertFromProvider(cents)!;

        cents.ShouldBe(12345L);
        roundTrip.ShouldBe(original);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1.00, 100)]
    [InlineData(10.50, 1050)]
    [InlineData(99.99, 9999)]
    [InlineData(100.00, 10000)]
    [InlineData(-50.25, -5025)]
    public void ConvertVariousDecimalsToCents(decimal value, long expectedCents)
    {
        var cents = (long)SqliteTypeConverters.DecimalToCents.ConvertToProvider(value)!;

        cents.ShouldBe(expectedCents);
    }

    [Fact]
    public void RoundDecimalToCentsWhenConverting()
    {
        var original = 123.456m;

        var cents = (long)SqliteTypeConverters.DecimalToCents.ConvertToProvider(original)!;

        cents.ShouldBe(12346L);
    }

    [Fact]
    public void ConvertLargeCentsBackToDecimal()
    {
        var cents = 1_000_000_00L;

        var result = (decimal)SqliteTypeConverters.DecimalToCents.ConvertFromProvider(cents)!;

        result.ShouldBe(1_000_000.00m);
    }

    [Fact]
    public void ConvertNullableDecimalWithValueToCentsAndBack()
    {
        decimal? original = 456.78m;

        var cents = (long?)SqliteTypeConverters.NullableDecimalToCents.ConvertToProvider(original);
        var roundTrip = (decimal?)SqliteTypeConverters.NullableDecimalToCents.ConvertFromProvider(cents);

        cents.ShouldBe(45678L);
        roundTrip.ShouldBe(original);
    }

    [Fact]
    public void ConvertNullDecimalToNullCents()
    {
        decimal? original = null;

        var cents = (long?)SqliteTypeConverters.NullableDecimalToCents.ConvertToProvider(original);

        cents.ShouldBeNull();
    }

    [Fact]
    public void ConvertNullCentsToNullDecimal()
    {
        long? cents = null;

        var result = (decimal?)SqliteTypeConverters.NullableDecimalToCents.ConvertFromProvider(cents);

        result.ShouldBeNull();
    }
}
