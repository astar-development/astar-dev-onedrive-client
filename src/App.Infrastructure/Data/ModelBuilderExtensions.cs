using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using App.Infrastructure.Data.Converters;

namespace App.Infrastructure.Data;

public static class ModelBuilderExtensions
{
    public static void UseSqliteFriendlyConversions(this ModelBuilder mb)
    {
        var targetEntities = new[]
        {
            typeof(App.Core.Entities.DriveItemRecord),
            typeof(App.Core.Entities.LocalFileRecord),
            typeof(App.Core.Entities.DeltaToken),
            typeof(App.Core.Entities.TransferLog)
        };

        foreach (var et in mb.Model.GetEntityTypes().Where(e => targetEntities.Contains(e.ClrType)))
            ApplyConversionsForEntity(mb, et);
    }

    static void ApplyConversionsForEntity(ModelBuilder mb, IMutableEntityType et)
    {
        var eb = mb.Entity(et.ClrType);

        foreach (var propInfo in et.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var pType = propInfo.PropertyType;

            // DateTimeOffset
            if (pType == typeof(DateTimeOffset))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");
            else if (Nullable.GetUnderlyingType(pType) == typeof(DateTimeOffset))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");

            // TimeSpan
            else if (pType == typeof(TimeSpan))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.TimeSpanToTicks).HasColumnType("INTEGER");
            else if (Nullable.GetUnderlyingType(pType) == typeof(TimeSpan))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableTimeSpanToTicks).HasColumnType("INTEGER");

            // Guid
            else if (pType == typeof(Guid))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.GuidToBytes).HasColumnType("BLOB");
            else if (Nullable.GetUnderlyingType(pType) == typeof(Guid))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableGuidToBytes).HasColumnType("BLOB");

            // decimal
            else if (pType == typeof(decimal))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DecimalToCents).HasColumnType("INTEGER");
            else if (Nullable.GetUnderlyingType(pType) == typeof(decimal))
                eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDecimalToCents).HasColumnType("INTEGER");

            // enums -> integer
            else if (pType.IsEnum)
                eb.Property(propInfo.Name).HasConversion<int>().HasColumnType("INTEGER");
            else if (Nullable.GetUnderlyingType(pType)?.IsEnum == true)
            {
                var enumType = Nullable.GetUnderlyingType(pType);
                if (enumType != null)
                {
                    var converterType = typeof(EnumToNumberConverter<,>).MakeGenericType(enumType, typeof(int));
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    eb.Property(propInfo.Name).HasConversion(converter).HasColumnType("INTEGER");
                }
            }
        }
    }
}
