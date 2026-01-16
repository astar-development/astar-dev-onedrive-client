using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;

public class WindowPreferencesConfiguration : IEntityTypeConfiguration<WindowPreferences>
{
    public void Configure(EntityTypeBuilder<WindowPreferences> builder)
    {
            _ = builder.HasKey(e => e.Id);
            _ = builder.Property(e => e.Width).HasDefaultValue(800);
            _ = builder.Property(e => e.Height).HasDefaultValue(600);
    }
}
