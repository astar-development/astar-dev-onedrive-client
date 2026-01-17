using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class LocalFileRecordConfiguration : IEntityTypeConfiguration<LocalFileRecord>
{
    public void Configure(EntityTypeBuilder<LocalFileRecord> builder)
    {
        _ = builder.ToTable("LocalFiles");
        _ = builder.HasKey(l => l.Id);

        _ = builder.Property("RelativePath").IsRequired();

        _ = builder.HasIndex("RelativePath");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
