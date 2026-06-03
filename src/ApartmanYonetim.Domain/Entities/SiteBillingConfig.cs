using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities;

public class SiteBillingConfig
{
    public int Id { get; set; } = 1;
    public decimal PricePerDaire { get; set; } = 15m;
    public decimal PricePerBlok { get; set; } = 50m;
    public decimal PricePerKisim { get; set; } = 100m;
    public decimal MinimumMonthly { get; set; } = 100m;
    public BillingPeriod DefaultPeriod { get; set; } = BillingPeriod.Monthly;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
