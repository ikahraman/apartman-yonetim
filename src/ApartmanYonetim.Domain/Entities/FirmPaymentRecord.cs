using ApartmanYonetim.Domain.Enums;

namespace ApartmanYonetim.Domain.Entities;

public class FirmPaymentRecord
{
    public int Id { get; set; }
    public string FirmSlug { get; set; } = default!;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public PaymentRecordStatus PaymentStatus { get; set; } = PaymentRecordStatus.Pending;
    public string? Notes { get; set; }
}
