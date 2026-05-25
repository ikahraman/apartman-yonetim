namespace ApartmanYonetim.Domain.Entities;

public class SiteProfile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CompanyId { get; set; }
    public ManagementCompany Company { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public int UnitCount { get; set; }
    public string DbFilePath { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Sözleşme bilgileri
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public decimal? MonthlyManagementFee { get; set; }
    public string? ContractNotes { get; set; }

    public List<UserSiteAccess> UserAccess { get; set; } = [];
}
