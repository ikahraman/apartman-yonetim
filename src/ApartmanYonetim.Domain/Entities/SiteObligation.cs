using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities;

public class SiteObligation
{
    public int Id { get; set; }
    public string FirmSlug { get; set; } = default!;
    public Guid SiteId { get; set; }
    public string SiteName { get; set; } = default!;
    public SiteType SiteType { get; set; }

    public int DaireCount { get; set; }
    public int BlokCount { get; set; }
    public int KisimCount { get; set; }

    public decimal MonthlyAmount { get; set; }
    public BillingPeriod BillingPeriod { get; set; } = BillingPeriod.Monthly;

    // Config değerleri oluşturma anında snapshot'lanır (audit için)
    public decimal PricePerDaire { get; set; }
    public decimal PricePerBlok { get; set; }
    public decimal PricePerKisim { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SiteObligationPayment> Payments { get; set; } = [];
}
