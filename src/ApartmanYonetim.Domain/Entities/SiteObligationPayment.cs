using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities;

public class SiteObligationPayment
{
    public int Id { get; set; }
    public int ObligationId { get; set; }
    public SiteObligation Obligation { get; set; } = default!;

    public string PeriodLabel { get; set; } = default!;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaymentDate { get; set; }

    public ObligationPaymentStatus Status { get; set; } = ObligationPaymentStatus.Pending;
    public string? Notes { get; set; }
    public string? RecordedBy { get; set; }
}
