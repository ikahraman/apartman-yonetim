using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteMeeting
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime MeetingDate { get; set; }
    public string? Location { get; set; }
    public MeetingType MeetingType { get; set; } = MeetingType.Ordinary;
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public string? AgendaItems { get; set; }
    public string CreatedBy { get; set; } = default!;
    public SiteMeetingMinutes? Minutes { get; set; }
}
