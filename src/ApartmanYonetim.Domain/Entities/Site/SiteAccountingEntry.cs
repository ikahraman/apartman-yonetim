using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteAccountingEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public AccountingEntryType Type { get; set; }
    public string Category { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = default!;
    public Guid? UnitId { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
