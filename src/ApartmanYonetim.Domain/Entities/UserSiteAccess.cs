namespace ApartmanYonetim.Domain.Entities;

public class UserSiteAccess
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string UserId { get; set; } = default!;
    public Guid SiteId { get; set; }
    public SiteProfile Site { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
