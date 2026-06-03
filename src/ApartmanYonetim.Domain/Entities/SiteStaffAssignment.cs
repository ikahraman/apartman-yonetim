namespace ApartmanYonetim.Domain.Entities;

public class SiteStaffAssignment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid StaffId { get; set; }
    public CompanyStaff Staff { get; set; } = default!;
    public Guid SiteId { get; set; }
    public SiteProfile Site { get; set; } = default!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedAt { get; set; }
    public string? AssignedByUserId { get; set; }
    public string? Notes { get; set; }
}
