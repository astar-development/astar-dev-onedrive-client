using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public class SyncConflictConfiguration : IEntityTypeConfiguration<SyncConflict>
{
    public void Configure(EntityTypeBuilder<SyncConflict> builder)
    {
            _ = builder.HasKey(e => e.Id);
            _ = builder.Property(e => e.Id).IsRequired();
            _ = builder.Property(e => e.AccountId).IsRequired();
            _ = builder.Property(e => e.FilePath).IsRequired();

            _ = builder.HasIndex(e => e.AccountId);
            _ = builder.HasIndex(e => new { e.AccountId, e.IsResolved });

            _ = builder.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}
