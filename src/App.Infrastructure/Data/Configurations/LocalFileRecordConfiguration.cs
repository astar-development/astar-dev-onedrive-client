using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Core.Entities;

namespace App.Infrastructure.Data.Configurations;

public sealed class LocalFileRecordConfiguration : IEntityTypeConfiguration<LocalFileRecord>
{
    public void Configure(EntityTypeBuilder<LocalFileRecord> b)
    {
        _ = b.ToTable("LocalFiles");
        _ = b.HasKey(l => l.Id);

        if (PropertyExists(typeof(LocalFileRecord), "RelativePath"))
            _ = b.Property("RelativePath").IsRequired();

        if (PropertyExists(typeof(LocalFileRecord), "RelativePath"))
            _ = b.HasIndex("RelativePath");
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
