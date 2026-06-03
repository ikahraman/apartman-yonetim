using ApartmanYonetim.Domain.Enums;

namespace ApartmanYonetim.Domain.Entities;

public class FirmSubscription
{
    public int Id { get; set; }
    public string FirmSlug { get; set; } = default!;
    public int FirmPackageId { get; set; }
    public FirmPackage Package { get; set; } = default!;
    public DateOnly ContractStartDate { get; set; }
    public DateOnly? ContractEndDate { get; set; }
    public decimal? CustomMonthlyPrice { get; set; }   // null → paketin fiyatı geçerli
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public string? Notes { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    public decimal EffectiveMonthlyPrice => CustomMonthlyPrice ?? Package?.MonthlyPrice ?? 0;
}
