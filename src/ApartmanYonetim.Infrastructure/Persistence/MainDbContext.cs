using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class MainDbContext(DbContextOptions<MainDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<FirmRegistration> FirmRegistrations => Set<FirmRegistration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FirmRegistration>(b =>
        {
            b.HasKey(f => f.Id);
            b.HasIndex(f => f.Slug).IsUnique();
            b.Property(f => f.Name).HasMaxLength(200).IsRequired();
            b.Property(f => f.Slug).HasMaxLength(100).IsRequired();
            b.Property(f => f.DbFilePath).HasMaxLength(500).IsRequired();
        });
    }
}
