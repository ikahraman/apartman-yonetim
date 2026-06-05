using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Egitim;

public class Kursiyer
{
    public int Id { get; set; }
    public int DonemId { get; set; }
    public string Ad { get; set; } = default!;
    public string Soyad { get; set; } = default!;
    public string FullName => $"{Ad} {Soyad}";
    public string Email { get; set; } = default!;
    public string? Telefon { get; set; }
    public string? Sehir { get; set; }
    public string? Meslek { get; set; }
    public OdemeDurumu OdemeDurumu { get; set; }
    public decimal OdenenTutar { get; set; }
    public DateOnly KayitTarihi { get; set; }
    public bool SertifikaVerildi { get; set; }
    public string? SertifikaNo { get; set; }
    public DateOnly? SertifikaTarihi { get; set; }
    public string? Notlar { get; set; }
    public EgitimDonemi Donem { get; set; } = default!;
    public List<DersTakibi> Takipler { get; set; } = [];
}
