using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AStar.Dev.OneDrive.Client.Core.Entities;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public sealed class DeltaTokenConfiguration : IEntityTypeConfiguration<DeltaToken>
{
    public void Configure(EntityTypeBuilder<DeltaToken> b)
    {
        _ = b.ToTable("DeltaTokens");
        _ = b.HasKey(t => t.Id);

        // We don't assume TokenValue exists; check first.
        if (PropertyExists(typeof(DeltaToken), "TokenValue"))
            _ = b.Property("TokenValue").HasColumnType("TEXT");

        // If there are timestamp properties (CreatedAt, ExpiresAt), conversions are applied by UseSqliteFriendlyConversions
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
