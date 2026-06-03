using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class MainDbContext(DbContextOptions<MainDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<FirmRegistration> FirmRegistrations => Set<FirmRegistration>();
    public DbSet<FirmPackage> FirmPackages => Set<FirmPackage>();
    public DbSet<FirmSubscription> FirmSubscriptions => Set<FirmSubscription>();
    public DbSet<FirmPaymentRecord> FirmPaymentRecords => Set<FirmPaymentRecord>();
    public DbSet<SiteBillingConfig> SiteBillingConfigs => Set<SiteBillingConfig>();
    public DbSet<SiteBillingTier> SiteBillingTiers => Set<SiteBillingTier>();
    public DbSet<SiteObligation> SiteObligations => Set<SiteObligation>();
    public DbSet<SiteObligationPayment> SiteObligationPayments => Set<SiteObligationPayment>();
    public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();

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
            b.Property(f => f.TaxNumber).HasMaxLength(20);
            b.Property(f => f.TaxOffice).HasMaxLength(200);
        });

        builder.Entity<SystemAuditLog>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.UserId).HasMaxLength(450).IsRequired();
            b.Property(l => l.UserEmail).HasMaxLength(256).IsRequired();
            b.Property(l => l.EntityType).HasMaxLength(100).IsRequired();
            b.Property(l => l.EntityId).HasMaxLength(100).IsRequired();
            b.HasIndex(l => new { l.EntityType, l.EntityId });
            b.HasIndex(l => l.Timestamp);
        });

        builder.Entity<FirmPackage>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).HasMaxLength(100).IsRequired();
            b.Property(p => p.MonthlyPrice).HasColumnType("decimal(10,2)");
        });

        builder.Entity<FirmSubscription>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.FirmSlug).IsUnique();
            b.Property(s => s.FirmSlug).HasMaxLength(100).IsRequired();
            b.Property(s => s.CustomMonthlyPrice).HasColumnType("decimal(10,2)");
            b.Property(s => s.Notes).HasMaxLength(2000);
            b.HasOne(s => s.Package).WithMany().HasForeignKey(s => s.FirmPackageId);
            b.Ignore(s => s.EffectiveMonthlyPrice);
        });

        builder.Entity<FirmPaymentRecord>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => new { p.FirmSlug, p.PeriodYear, p.PeriodMonth }).IsUnique();
            b.Property(p => p.FirmSlug).HasMaxLength(100).IsRequired();
            b.Property(p => p.AmountDue).HasColumnType("decimal(10,2)");
            b.Property(p => p.AmountPaid).HasColumnType("decimal(10,2)");
            b.Property(p => p.Notes).HasMaxLength(1000);
        });

        builder.Entity<SiteBillingTier>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.MonthlyAmount).HasColumnType("decimal(10,2)");
        });

        builder.Entity<SiteBillingConfig>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.PricePerDaire).HasColumnType("decimal(10,2)");
            b.Property(c => c.PricePerBlok).HasColumnType("decimal(10,2)");
            b.Property(c => c.PricePerKisim).HasColumnType("decimal(10,2)");
            b.Property(c => c.MinimumMonthly).HasColumnType("decimal(10,2)");
            b.Property(c => c.UpdatedBy).HasMaxLength(200);
        });

        builder.Entity<SiteObligation>(b =>
        {
            b.HasKey(o => o.Id);
            b.HasIndex(o => o.SiteId).IsUnique();
            b.Property(o => o.FirmSlug).HasMaxLength(100).IsRequired();
            b.Property(o => o.SiteName).HasMaxLength(200).IsRequired();
            b.Property(o => o.MonthlyAmount).HasColumnType("decimal(10,2)");
            b.Property(o => o.PricePerDaire).HasColumnType("decimal(10,2)");
            b.Property(o => o.PricePerBlok).HasColumnType("decimal(10,2)");
            b.Property(o => o.PricePerKisim).HasColumnType("decimal(10,2)");
            b.HasMany(o => o.Payments).WithOne(p => p.Obligation).HasForeignKey(p => p.ObligationId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SiteObligationPayment>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.PeriodLabel).HasMaxLength(100).IsRequired();
            b.Property(p => p.AmountDue).HasColumnType("decimal(10,2)");
            b.Property(p => p.AmountPaid).HasColumnType("decimal(10,2)");
            b.Property(p => p.Notes).HasMaxLength(1000);
            b.Property(p => p.RecordedBy).HasMaxLength(200);
        });
    }
}
