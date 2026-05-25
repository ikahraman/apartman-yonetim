using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteFeePayment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UnitId { get; set; }
    public Guid ScheduleId { get; set; }
    public string PeriodLabel { get; set; } = default!;
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? PaidDate { get; set; }
    public decimal? PaidAmount { get; set; }
    public FeePaymentStatus Status { get; set; } = FeePaymentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
