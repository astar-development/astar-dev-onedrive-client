using System.Reflection;
using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class DeltaTokenConfiguration : IEntityTypeConfiguration<DeltaToken>
{
    public void Configure(EntityTypeBuilder<DeltaToken> builder)
    {
        _ = builder.ToTable("DeltaTokens");
        _ = builder.HasKey(t => t.Id);

        _ = builder.Property("TokenValue").HasColumnType("TEXT");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
