using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class DriveItemRecordConfiguration : IEntityTypeConfiguration<DriveItemRecord>
{
    public void Configure(EntityTypeBuilder<DriveItemRecord> builder)
    {
        _ = builder.ToTable("DriveItems");
        _ = builder.HasKey(d => d.Id);

        _ = builder.Property("RelativePath").IsRequired();

        _ = builder.HasIndex("DriveItemId");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
