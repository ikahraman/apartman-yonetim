using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Domain.Entities.Egitim;

public class EgitimDonemi
{
    public int Id { get; set; }
    public int EgitimId { get; set; }
    public string Ad { get; set; } = default!;
    public DateOnly BaslangicTarihi { get; set; }
    public DateOnly BitisTarihi { get; set; }
    public int Kontenjan { get; set; }
    public decimal Fiyat { get; set; }
    public EgitimTuru Tur { get; set; }
    public string? Konum { get; set; }
    public string? OnlinePlatform { get; set; }
    public DonemDurumu Durum { get; set; }
    public Egitim Egitim { get; set; } = default!;
    public List<DersProgrami> DersProgramlari { get; set; } = [];
    public List<Kursiyer> Kursiyerler { get; set; } = [];
}
