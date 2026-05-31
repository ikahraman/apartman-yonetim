using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteFeeSchedule
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = default!;
    public decimal Amount { get; set; }
    public FeePeriod Period { get; set; } = FeePeriod.Monthly;
    public int DueDay { get; set; } = 10;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DistributionType DistributionType { get; set; } = DistributionType.EsitPay;
    public UnitType? AppliesToUnitType { get; set; }
}
