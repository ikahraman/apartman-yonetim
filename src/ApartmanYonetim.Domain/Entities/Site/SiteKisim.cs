namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteKisim
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public ICollection<SiteBlock> Blocks { get; set; } = [];
}
