namespace ApartmanYonetim.Domain.Entities.Site;

public class SiteDaireType
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = default!;   // "1+1", "2+1", "Dubleks"
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }             // sistem varsayılanları silinemez
}
