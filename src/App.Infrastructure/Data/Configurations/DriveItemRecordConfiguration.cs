using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Core.Entities;

namespace App.Infrastructure.Data.Configurations;

public sealed class DriveItemRecordConfiguration : IEntityTypeConfiguration<DriveItemRecord>
{
    public void Configure(EntityTypeBuilder<DriveItemRecord> b)
    {
        b.ToTable("DriveItems");
        b.HasKey(d => d.Id);

        // Use reflection to avoid compile-time errors if the property doesn't exist
        if (PropertyExists(typeof(DriveItemRecord), "RelativePath"))
            b.Property("RelativePath").IsRequired();

        if (PropertyExists(typeof(DriveItemRecord), "DriveItemId"))
            b.HasIndex("DriveItemId");
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
