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
        b.ToTable("LocalFiles");
        b.HasKey(l => l.Id);

        if (PropertyExists(typeof(LocalFileRecord), "RelativePath"))
            b.Property("RelativePath").IsRequired();

        if (PropertyExists(typeof(LocalFileRecord), "RelativePath"))
            b.HasIndex("RelativePath");
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
