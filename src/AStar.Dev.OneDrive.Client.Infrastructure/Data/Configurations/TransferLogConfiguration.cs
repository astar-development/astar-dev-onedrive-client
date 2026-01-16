using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class TransferLogConfiguration : IEntityTypeConfiguration<TransferLog>
{
    public void Configure(EntityTypeBuilder<TransferLog> builder)
    {
        _ = builder.ToTable("TransferLogs");
        _ = builder.HasKey(t => t.Id);

        _ = builder.Property("Status").HasColumnType("TEXT");

        _ = builder.Property("BytesTransferred").HasColumnType("INTEGER");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
