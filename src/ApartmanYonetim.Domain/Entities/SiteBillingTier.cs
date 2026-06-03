namespace ApartmanYonetim.Domain.Entities;

public class SiteBillingTier
{
    public int Id { get; set; }
    public int MinDaire { get; set; }
    public int? MaxDaire { get; set; }  // null = sınırsız
    public decimal MonthlyAmount { get; set; }
    public int DisplayOrder { get; set; }
}
