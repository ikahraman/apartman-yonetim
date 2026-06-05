using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record EgitimDto(int Id, string Ad, string? Aciklama, string? Hedefler, string? Gereksinimler, string? SertifikaAdi, bool IsActive);

public record EgitimDonemDto(
    int Id, int EgitimId, string EgitimAd, string Ad,
    DateOnly BaslangicTarihi, DateOnly BitisTarihi,
    int Kontenjan, decimal Fiyat, EgitimTuru Tur,
    string? Konum, string? OnlinePlatform, DonemDurumu Durum,
    int KursiyerSayisi, int DersSayisi);

public record DersProgramiDto(
    int Id, int DonemId, int SiraNo, string Baslik, string? Aciklama,
    DateTime DersTarihi, int SureDakika,
    string? IcerikMetin, string? IcerikLink, string? VideoLink,
    int KatilimSayisi);

public record KursiyerDto(
    int Id, int DonemId, string Ad, string Soyad, string FullName,
    string Email, string? Telefon, string? Sehir, string? Meslek,
    OdemeDurumu OdemeDurumu, decimal OdenenTutar, DateOnly KayitTarihi,
    bool SertifikaVerildi, string? SertifikaNo, DateOnly? SertifikaTarihi,
    string? Notlar, int KatilimSayisi, int ToplamDers);

public record DersTakibiDto(int Id, int KursiyerId, string KursiyerAd, int DersProgramiId, string DersBaslik, bool Katildi, string? Not);

public record KursiyerKayitCommand(string Ad, string Soyad, string Email, string? Telefon, string? Sehir, string? Meslek, string? Notlar);
public record DersProgramiCommand(string Baslik, string? Aciklama, DateTime DersTarihi, int SureDakika, string? IcerikMetin, string? IcerikLink, string? VideoLink);
public record DonemCommand(string Ad, DateOnly BaslangicTarihi, DateOnly BitisTarihi, int Kontenjan, decimal Fiyat, EgitimTuru Tur, string? Konum, string? OnlinePlatform, DonemDurumu Durum);

public interface IEgitimService
{
    Task<List<EgitimDto>> GetEgitimlerAsync();
    Task<EgitimDto?> GetEgitimAsync(int id);

    Task<List<EgitimDonemDto>> GetDonemleriAsync(int egitimId);
    Task<EgitimDonemDto?> GetDonemAsync(int donemId);
    Task UpdateDonemAsync(int donemId, DonemCommand cmd);

    Task<List<DersProgramiDto>> GetDersProgramiAsync(int donemId);
    Task<DersProgramiDto> AddDersAsync(int donemId, DersProgramiCommand cmd);
    Task UpdateDersAsync(int dersId, DersProgramiCommand cmd);
    Task DeleteDersAsync(int dersId);

    Task<List<KursiyerDto>> GetKursiyerlerAsync(int donemId);
    Task<KursiyerDto> AddKursiyerAsync(int donemId, KursiyerKayitCommand cmd);
    Task UpdateKursiyerAsync(int kursiyerId, KursiyerKayitCommand cmd);
    Task UpdateOdemeAsync(int kursiyerId, OdemeDurumu durum, decimal odenenTutar);
    Task SertifikaVerAsync(int kursiyerId);
    Task DeleteKursiyerAsync(int kursiyerId);

    Task<List<DersTakibiDto>> GetTakiplerAsync(int donemId);
    Task SetKatilimAsync(int kursiyerId, int dersId, bool katildi);
}
