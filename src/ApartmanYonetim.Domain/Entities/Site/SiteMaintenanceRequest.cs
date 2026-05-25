using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteMaintenanceRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? UnitId { get; set; }
    public string? ReporterName { get; set; }
    public string? ReporterPhone { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}
