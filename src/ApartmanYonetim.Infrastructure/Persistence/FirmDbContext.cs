using ApartmanYonetim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class FirmDbContext(DbContextOptions<FirmDbContext> options) : DbContext(options)
{
    public DbSet<ManagementCompany> Companies => Set<ManagementCompany>();
    public DbSet<SiteProfile> Sites => Set<SiteProfile>();
    public DbSet<UserSiteAccess> UserSiteAccess => Set<UserSiteAccess>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ManagementCompany>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Name).HasMaxLength(200).IsRequired();
            b.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            b.HasMany(c => c.Sites).WithOne(s => s.Company).HasForeignKey(s => s.CompanyId);
        });

        builder.Entity<SiteProfile>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.Slug).IsUnique();
            b.Property(s => s.Name).HasMaxLength(200).IsRequired();
            b.Property(s => s.Slug).HasMaxLength(100).IsRequired();
            b.Property(s => s.DbFilePath).HasMaxLength(500).IsRequired();
            b.HasMany(s => s.UserAccess).WithOne(a => a.Site).HasForeignKey(a => a.SiteId);
        });

        builder.Entity<UserSiteAccess>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => new { a.UserId, a.SiteId }).IsUnique();
        });
    }
}
