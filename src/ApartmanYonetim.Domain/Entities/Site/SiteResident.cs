using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteResident
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UnitId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName => $"{FirstName} {LastName}";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public ResidencyType ResidencyType { get; set; }
    public DateOnly MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}
