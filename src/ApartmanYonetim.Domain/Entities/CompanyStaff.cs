using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities;

public class CompanyStaff
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CompanyId { get; set; }
    public ManagementCompany Company { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public StaffRole Role { get; set; } = StaffRole.SiteManager;
    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedAt { get; set; }
    public List<SiteStaffAssignment> SiteAssignments { get; set; } = [];
}
