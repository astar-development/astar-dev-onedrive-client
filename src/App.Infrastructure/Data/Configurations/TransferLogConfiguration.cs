using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Core.Entities;

namespace App.Infrastructure.Data.Configurations;

public sealed class TransferLogConfiguration : IEntityTypeConfiguration<TransferLog>
{
    public void Configure(EntityTypeBuilder<TransferLog> b)
    {
        b.ToTable("TransferLogs");
        b.HasKey(t => t.Id);

        if (PropertyExists(typeof(TransferLog), "Status"))
            b.Property("Status").HasColumnType("TEXT");

        // BytesTransferred may not exist on your TransferLog; check before mapping
        if (PropertyExists(typeof(TransferLog), "BytesTransferred"))
            b.Property("BytesTransferred").HasColumnType("INTEGER");
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
