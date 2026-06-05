namespace ApartmanYonetim.Domain.Entities.Egitim;

public class Egitim
{
    public int Id { get; set; }
    public string Ad { get; set; } = default!;
    public string? Aciklama { get; set; }
    public string? Hedefler { get; set; }
    public string? Gereksinimler { get; set; }
    public string? SertifikaAdi { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime OlusturulmaTarihi { get; set; } = DateTime.UtcNow;
    public List<EgitimDonemi> Donemler { get; set; } = [];
}
