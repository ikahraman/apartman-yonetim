namespace ApartmanYonetim.Domain.Entities.Egitim;

public class DersProgrami
{
    public int Id { get; set; }
    public int DonemId { get; set; }
    public int SiraNo { get; set; }
    public string Baslik { get; set; } = default!;
    public string? Aciklama { get; set; }
    public DateTime DersTarihi { get; set; }
    public int SureDakika { get; set; } = 90;
    public string? IcerikMetin { get; set; }
    public string? IcerikLink { get; set; }
    public string? VideoLink { get; set; }
    public EgitimDonemi Donem { get; set; } = default!;
    public List<DersTakibi> Takipler { get; set; } = [];
}
