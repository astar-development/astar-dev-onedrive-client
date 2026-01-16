using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        _ = builder.HasKey(e => e.AccountId);
        _ = builder.Property(e => e.AccountId).IsRequired();
        _ = builder.Property(e => e.DisplayName).IsRequired();
        _ = builder.Property(e => e.LocalSyncPath).IsRequired();
        _ = builder.HasIndex(e => e.LocalSyncPath).IsUnique();
    }
}
