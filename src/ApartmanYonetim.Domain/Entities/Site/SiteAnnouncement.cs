namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteAnnouncement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public bool IsUrgent { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string CreatedBy { get; set; } = default!;
}
