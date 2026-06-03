namespace ApartmanYonetim.Domain.Entities;

public class ManagementCompany
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<SiteProfile> Sites { get; set; } = [];
    public List<CompanyStaff> Staff { get; set; } = [];
}
