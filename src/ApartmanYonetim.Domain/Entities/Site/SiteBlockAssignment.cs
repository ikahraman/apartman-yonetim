namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteBlockAssignment
{
    public int Id { get; set; }
    public Guid BlockId { get; set; }
    public SiteBlock Block { get; set; } = default!;
    public string ManagerUserId { get; set; } = default!;
    public string ManagerDisplayName { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent => EndDate == null || EndDate >= DateOnly.FromDateTime(DateTime.Today);
}
