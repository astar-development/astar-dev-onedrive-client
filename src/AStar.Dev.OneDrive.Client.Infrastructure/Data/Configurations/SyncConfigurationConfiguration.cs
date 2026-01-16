using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public class SyncConfigurationConfiguration : IEntityTypeConfiguration<SyncConfiguration>
{
    public void Configure(EntityTypeBuilder<SyncConfiguration> builder)
    {
            _ = builder.HasKey(e => e.Id);
            _ = builder.Property(e => e.AccountId).IsRequired();
            _ = builder.Property(e => e.FolderPath).IsRequired();
            _ = builder.HasIndex(e => new { e.AccountId, e.FolderPath });
            
            _ = builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}
