using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteUnit
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Number { get; set; } = default!;
    public string? Block { get; set; }
    public int? Floor { get; set; }
    public decimal? SquareMeters { get; set; }
    public OccupancyType OccupancyType { get; set; } = OccupancyType.Empty;
}
