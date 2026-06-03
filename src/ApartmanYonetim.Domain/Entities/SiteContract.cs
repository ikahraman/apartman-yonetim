using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities;

public class SiteContract
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid SiteId { get; set; }
    public SiteProfile Site { get; set; } = default!;
    public string? ContractNumber { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public ContractScope Scope { get; set; } = ContractScope.Tumu;
    public ManagementFeeType FeeType { get; set; } = ManagementFeeType.Fixed;
    public decimal MonthlyFee { get; set; }
    public string? TerminationReason { get; set; }
    public string? Notes { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
}
