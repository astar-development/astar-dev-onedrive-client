using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data;

public static class ModelBuilderExtensions
{
    public static void UseSqliteFriendlyConversions(this ModelBuilder mb)
    {
        Type[] targetEntities =
        [
            typeof(Core.Entities.DriveItemRecord),
            typeof(Core.Entities.LocalFileRecord),
            typeof(Core.Entities.DeltaToken),
            typeof(Core.Entities.TransferLog)
        ];

        foreach(IMutableEntityType? et in mb.Model.GetEntityTypes().Where(e => targetEntities.Contains(e.ClrType)))
            ApplyConversionsForEntity(mb, et);
    }

    static void ApplyConversionsForEntity(ModelBuilder mb, IMutableEntityType et)
    {
        EntityTypeBuilder eb = mb.Entity(et.ClrType);

        foreach(PropertyInfo propInfo in et.ClrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Type pType = propInfo.PropertyType;

            // DateTimeOffset
            if(pType == typeof(DateTimeOffset))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");
            else if(Nullable.GetUnderlyingType(pType) == typeof(DateTimeOffset))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDateTimeOffsetToTicks).HasColumnType("INTEGER").HasColumnName(propInfo.Name + "_Ticks");

            // TimeSpan
            else if(pType == typeof(TimeSpan))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.TimeSpanToTicks).HasColumnType("INTEGER");
            else if(Nullable.GetUnderlyingType(pType) == typeof(TimeSpan))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableTimeSpanToTicks).HasColumnType("INTEGER");

            // Guid
            else if(pType == typeof(Guid))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.GuidToBytes).HasColumnType("BLOB");
            else if(Nullable.GetUnderlyingType(pType) == typeof(Guid))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableGuidToBytes).HasColumnType("BLOB");

            // decimal
            else if(pType == typeof(decimal))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.DecimalToCents).HasColumnType("INTEGER");
            else if(Nullable.GetUnderlyingType(pType) == typeof(decimal))
                _ = eb.Property(propInfo.Name).HasConversion(SqliteTypeConverters.NullableDecimalToCents).HasColumnType("INTEGER");

            // enums -> integer
            else if(pType.IsEnum)
                _ = eb.Property(propInfo.Name).HasConversion<int>().HasColumnType("INTEGER");
            else if(Nullable.GetUnderlyingType(pType)?.IsEnum == true)
            {
                Type? enumType = Nullable.GetUnderlyingType(pType);
                if(enumType != null)
                {
                    Type converterType = typeof(EnumToNumberConverter<,>).MakeGenericType(enumType, typeof(int));
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    _ = eb.Property(propInfo.Name).HasConversion(converter).HasColumnType("INTEGER");
                }
            }
        }
    }
}
