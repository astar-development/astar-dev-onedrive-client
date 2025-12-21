using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Core.Entities;

namespace App.Infrastructure.Data.Configurations;

public sealed class DeltaTokenConfiguration : IEntityTypeConfiguration<DeltaToken>
{
    public void Configure(EntityTypeBuilder<DeltaToken> b)
    {
        b.ToTable("DeltaTokens");
        b.HasKey(t => t.Id);

        // We don't assume TokenValue exists; check first.
        if (PropertyExists(typeof(DeltaToken), "TokenValue"))
            b.Property("TokenValue").HasColumnType("TEXT");

        // If there are timestamp properties (CreatedAt, ExpiresAt), conversions are applied by UseSqliteFriendlyConversions
    }

    static bool PropertyExists(Type t, string name) => t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
}
