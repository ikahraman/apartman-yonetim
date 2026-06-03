namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteBlock
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public int? FloorCount { get; set; }
    public int? UnitCount { get; set; }
    public Guid? KisimId { get; set; }
    public SiteKisim? Kisim { get; set; }
    public ICollection<SiteUnit> Units { get; set; } = [];
    public ICollection<SiteBlockAssignment> Assignments { get; set; } = [];
}
