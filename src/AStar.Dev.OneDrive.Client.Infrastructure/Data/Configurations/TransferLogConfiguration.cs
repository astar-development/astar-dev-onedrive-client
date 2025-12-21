using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class TransferLogConfiguration : IEntityTypeConfiguration<TransferLog>
{
    public void Configure(EntityTypeBuilder<TransferLog> b)
    {
        _ = b.ToTable("TransferLogs");
        _ = b.HasKey(t => t.Id);

        if(PropertyExists(typeof(TransferLog), "Status"))
            _ = b.Property("Status").HasColumnType("TEXT");

        // BytesTransferred may not exist on your TransferLog; check before mapping
        if(PropertyExists(typeof(TransferLog), "BytesTransferred"))
            _ = b.Property("BytesTransferred").HasColumnType("INTEGER");
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
