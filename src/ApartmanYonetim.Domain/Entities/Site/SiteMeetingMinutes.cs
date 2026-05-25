namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteMeetingMinutes
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid MeetingId { get; set; }
    public string Content { get; set; } = default!;
    public int AttendeeCount { get; set; }
    public string? Decisions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
