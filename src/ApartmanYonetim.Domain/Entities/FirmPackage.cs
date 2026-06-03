namespace ApartmanYonetim.Domain.Entities;

public class FirmPackage
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int MinSiteCount { get; set; }
    public int? MaxSiteCount { get; set; }   // null = sınırsız
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
