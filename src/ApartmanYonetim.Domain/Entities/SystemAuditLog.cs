using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities;

public class SystemAuditLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = default!;
    public string UserEmail { get; set; } = default!;
    public AuditActionType ActionType { get; set; }
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string? Changes { get; set; }
}
