using ApartmanYonetim.Domain.Entities.Site;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class SiteDbContext(DbContextOptions<SiteDbContext> options) : DbContext(options)
{
    public DbSet<SiteKisim> Kisimlar => Set<SiteKisim>();
    public DbSet<SiteBlock> Blocks => Set<SiteBlock>();
    public DbSet<SiteUnit> Units => Set<SiteUnit>();
    public DbSet<SiteResident> Residents => Set<SiteResident>();
    public DbSet<SiteFeeSchedule> FeeSchedules => Set<SiteFeeSchedule>();
    public DbSet<SiteFeePayment> FeePayments => Set<SiteFeePayment>();
    public DbSet<SiteAnnouncement> Announcements => Set<SiteAnnouncement>();
    public DbSet<SiteMaintenanceRequest> MaintenanceRequests => Set<SiteMaintenanceRequest>();
    public DbSet<SiteMeeting> Meetings => Set<SiteMeeting>();
    public DbSet<SiteMeetingMinutes> MeetingMinutes => Set<SiteMeetingMinutes>();
    public DbSet<SiteAccountingEntry> AccountingEntries => Set<SiteAccountingEntry>();
    public DbSet<SiteBlockAssignment> BlockAssignments => Set<SiteBlockAssignment>();
    public DbSet<SiteDaireType> DaireTypes => Set<SiteDaireType>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.Entity<SiteKisim>(b => b.HasMany(k => k.Blocks).WithOne(bl => bl.Kisim).HasForeignKey(bl => bl.KisimId).OnDelete(DeleteBehavior.SetNull));
        m.Entity<SiteUnit>(b =>
        {
            b.HasOne(u => u.BlockRef).WithMany(bl => bl.Units).HasForeignKey(u => u.BlockId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(u => u.DaireType).WithMany().HasForeignKey(u => u.DaireTypeId).OnDelete(DeleteBehavior.SetNull);
        });
        m.Entity<SiteDaireType>(b =>
        {
            b.HasKey(d => d.Id);
            b.Property(d => d.Name).HasMaxLength(50).IsRequired();
        });
        m.Entity<SiteBlockAssignment>(b =>
        {
            b.HasKey(a => a.Id);
            b.HasOne(a => a.Block).WithMany(bl => bl.Assignments).HasForeignKey(a => a.BlockId).OnDelete(DeleteBehavior.Cascade);
            b.Property(a => a.ManagerUserId).HasMaxLength(450).IsRequired();
            b.Property(a => a.ManagerDisplayName).HasMaxLength(200).IsRequired();
        });
        m.Entity<SiteResident>(b => b.HasIndex(r => r.UnitId));
        m.Entity<SiteFeePayment>(b =>
            b.HasIndex(p => new { p.ScheduleId, p.UnitId, p.PeriodLabel }).IsUnique());
        m.Entity<SiteMeeting>(b =>
            b.HasOne(x => x.Minutes).WithOne()
             .HasForeignKey<SiteMeetingMinutes>(mm => mm.MeetingId));
    }
}
