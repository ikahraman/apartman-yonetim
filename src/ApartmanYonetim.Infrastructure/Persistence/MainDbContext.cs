using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Entities.Egitim;
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
    public DbSet<SiteObligation> SiteObligations => Set<SiteObligation>();
    public DbSet<SiteObligationPayment> SiteObligationPayments => Set<SiteObligationPayment>();
    public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();

    // Eğitim modülü
    public DbSet<Egitim> Egitimler => Set<Egitim>();
    public DbSet<EgitimDonemi> EgitimDonemleri => Set<EgitimDonemi>();
    public DbSet<DersProgrami> DersProgramlari => Set<DersProgrami>();
    public DbSet<Kursiyer> Kursiyerler => Set<Kursiyer>();
    public DbSet<DersTakibi> DersTakipleri => Set<DersTakibi>();

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

        builder.Entity<Egitim>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Ad).HasMaxLength(300).IsRequired();
        });

        builder.Entity<EgitimDonemi>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Ad).HasMaxLength(200).IsRequired();
            b.Property(d => d.Fiyat).HasColumnType("decimal(10,2)");
            b.HasOne(d => d.Egitim).WithMany(e => e.Donemler).HasForeignKey(d => d.EgitimId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DersProgrami>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Baslik).HasMaxLength(300).IsRequired();
            b.HasOne(d => d.Donem).WithMany(dn => dn.DersProgramlari).HasForeignKey(d => d.DonemId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Kursiyer>(b =>
        {
            b.HasKey(k => k.Id);
            b.Property(k => k.Ad).HasMaxLength(100).IsRequired();
            b.Property(k => k.Soyad).HasMaxLength(100).IsRequired();
            b.Property(k => k.Email).HasMaxLength(256).IsRequired();
            b.Property(k => k.OdenenTutar).HasColumnType("decimal(10,2)");
            b.Ignore(k => k.FullName);
            b.HasOne(k => k.Donem).WithMany(d => d.Kursiyerler).HasForeignKey(k => k.DonemId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DersTakibi>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => new { t.KursiyerId, t.DersProgramiId }).IsUnique();
            b.HasOne(t => t.Kursiyer).WithMany(k => k.Takipler).HasForeignKey(t => t.KursiyerId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(t => t.DersProgrami).WithMany(d => d.Takipler).HasForeignKey(t => t.DersProgramiId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
