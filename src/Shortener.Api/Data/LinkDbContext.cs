using Microsoft.EntityFrameworkCore;
using Shortener.Api.Domain;

namespace Shortener.Api.Data;

public sealed class LinkDbContext(DbContextOptions<LinkDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var link = modelBuilder.Entity<Link>();
        link.ToTable("Links");
        link.HasKey(item => item.Id);
        link.Property(item => item.Code).HasMaxLength(7).IsRequired();
        link.HasIndex(item => item.Code).IsUnique();
        link.Property(item => item.TargetUrl).HasMaxLength(2048).IsRequired();
        link.Property(item => item.CreatedAtUtc).IsRequired();
        link.Property(item => item.ExpiresAtUtc);
        link.Property(item => item.AccessCount).HasDefaultValue(0L).IsRequired();
    }
}
