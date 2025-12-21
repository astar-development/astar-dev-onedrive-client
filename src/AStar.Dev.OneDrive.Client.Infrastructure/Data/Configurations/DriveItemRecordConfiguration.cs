using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class DriveItemRecordConfiguration : IEntityTypeConfiguration<DriveItemRecord>
{
    public void Configure(EntityTypeBuilder<DriveItemRecord> b)
    {
        _ = b.ToTable("DriveItems");
        _ = b.HasKey(d => d.Id);

        // Use reflection to avoid compile-time errors if the property doesn't exist
        if(PropertyExists(typeof(DriveItemRecord), "RelativePath"))
            _ = b.Property("RelativePath").IsRequired();

        if(PropertyExists(typeof(DriveItemRecord), "DriveItemId"))
            _ = b.HasIndex("DriveItemId");
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
