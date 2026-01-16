using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public class FileMetadataConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
            _ = builder.HasKey(e => e.Id);
            _ = builder.Property(e => e.Id).IsRequired();
            _ = builder.Property(e => e.AccountId).IsRequired();
            _ = builder.Property(e => e.Name).IsRequired();
            _ = builder.Property(e => e.Path).IsRequired();
            _ = builder.Property(e => e.LocalPath).IsRequired();

            _ = builder.HasIndex(e => e.AccountId);
            _ = builder.HasIndex(e => new { e.AccountId, e.Path });

            _ = builder.HasOne<Account>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}
