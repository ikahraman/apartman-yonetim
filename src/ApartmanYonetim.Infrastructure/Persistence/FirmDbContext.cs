using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class FirmDbContext(DbContextOptions<FirmDbContext> options) : DbContext(options)
{
    public DbSet<ManagementCompany> Companies => Set<ManagementCompany>();
    public DbSet<SiteProfile> Sites => Set<SiteProfile>();
    public DbSet<CompanyStaff> CompanyStaff => Set<CompanyStaff>();
    public DbSet<SiteStaffAssignment> SiteStaffAssignments => Set<SiteStaffAssignment>();
    public DbSet<SiteContract> SiteContracts => Set<SiteContract>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ManagementCompany>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Name).HasMaxLength(200).IsRequired();
            b.Property(c => c.Slug).HasMaxLength(100).IsRequired();
            b.Property(c => c.Email).HasMaxLength(200);
            b.Property(c => c.Phone).HasMaxLength(50);
            b.Property(c => c.ContactPerson).HasMaxLength(200);
            b.Property(c => c.Website).HasMaxLength(300);
            b.Property(c => c.LogoUrl).HasMaxLength(500);
            b.HasMany(c => c.Sites).WithOne(s => s.Company).HasForeignKey(s => s.CompanyId);
            b.HasMany(c => c.Staff).WithOne(s => s.Company).HasForeignKey(s => s.CompanyId);
        });

        builder.Entity<SiteProfile>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => s.Slug).IsUnique();
            b.Property(s => s.Name).HasMaxLength(200).IsRequired();
            b.Property(s => s.Slug).HasMaxLength(100).IsRequired();
            b.Property(s => s.DbFilePath).HasMaxLength(500).IsRequired();
            b.HasMany(s => s.Contracts).WithOne(c => c.Site).HasForeignKey(c => c.SiteId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(s => s.StaffAssignments).WithOne(a => a.Site).HasForeignKey(a => a.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CompanyStaff>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasIndex(s => new { s.CompanyId, s.UserId }).IsUnique();
            b.Property(s => s.UserId).HasMaxLength(450).IsRequired();
            b.Property(s => s.CreatedByUserId).HasMaxLength(450);
            b.HasMany(s => s.SiteAssignments).WithOne(a => a.Staff).HasForeignKey(a => a.StaffId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SiteStaffAssignment>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasIndex(a => new { a.StaffId, a.SiteId });
            b.Property(a => a.AssignedByUserId).HasMaxLength(450);
            b.Property(a => a.Notes).HasMaxLength(500);
        });

        builder.Entity<SiteContract>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.ContractNumber).HasMaxLength(100);
            b.Property(c => c.MonthlyFee).HasColumnType("decimal(12,2)");
            b.Property(c => c.Notes).HasMaxLength(2000);
            b.Property(c => c.TerminationReason).HasMaxLength(1000);
            b.Property(c => c.CreatedByUserId).HasMaxLength(450);
        });
    }
}
