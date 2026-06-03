namespace ApartmanYonetim.Domain.Entities;

public class FirmRegistration
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string DbFilePath { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // SuperAdmin-only alanlar
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
}
