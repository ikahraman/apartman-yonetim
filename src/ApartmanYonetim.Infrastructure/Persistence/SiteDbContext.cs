using ApartmanYonetim.Domain.Entities.Site;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class SiteDbContext(DbContextOptions<SiteDbContext> options) : DbContext(options)
{
    public DbSet<SiteUnit> Units => Set<SiteUnit>();
    public DbSet<SiteResident> Residents => Set<SiteResident>();
    public DbSet<SiteFeeSchedule> FeeSchedules => Set<SiteFeeSchedule>();
    public DbSet<SiteFeePayment> FeePayments => Set<SiteFeePayment>();
    public DbSet<SiteAnnouncement> Announcements => Set<SiteAnnouncement>();
    public DbSet<SiteMaintenanceRequest> MaintenanceRequests => Set<SiteMaintenanceRequest>();
    public DbSet<SiteMeeting> Meetings => Set<SiteMeeting>();
    public DbSet<SiteMeetingMinutes> MeetingMinutes => Set<SiteMeetingMinutes>();
    public DbSet<SiteAccountingEntry> AccountingEntries => Set<SiteAccountingEntry>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        m.Entity<SiteResident>(b => b.HasIndex(r => r.UnitId));
        m.Entity<SiteFeePayment>(b =>
            b.HasIndex(p => new { p.ScheduleId, p.UnitId, p.PeriodLabel }).IsUnique());
        m.Entity<SiteMeeting>(b =>
            b.HasOne(x => x.Minutes).WithOne()
             .HasForeignKey<SiteMeetingMinutes>(mm => mm.MeetingId));
    }
}
