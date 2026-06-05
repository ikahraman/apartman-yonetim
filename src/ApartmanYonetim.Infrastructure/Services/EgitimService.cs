using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Egitim;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class EgitimService(MainDbContext db) : IEgitimService
{
    public async Task<List<EgitimDto>> GetEgitimlerAsync() =>
        await db.Egitimler.Where(e => e.IsActive)
            .Select(e => new EgitimDto(e.Id, e.Ad, e.Aciklama, e.Hedefler, e.Gereksinimler, e.SertifikaAdi, e.IsActive))
            .ToListAsync();

    public async Task<EgitimDto?> GetEgitimAsync(int id) =>
        await db.Egitimler.Where(e => e.Id == id)
            .Select(e => new EgitimDto(e.Id, e.Ad, e.Aciklama, e.Hedefler, e.Gereksinimler, e.SertifikaAdi, e.IsActive))
            .FirstOrDefaultAsync();

    public async Task<List<EgitimDonemDto>> GetDonemleriAsync(int egitimId) =>
        await db.EgitimDonemleri
            .Where(d => d.EgitimId == egitimId)
            .OrderByDescending(d => d.BaslangicTarihi)
            .Select(d => new EgitimDonemDto(
                d.Id, d.EgitimId, d.Egitim.Ad, d.Ad,
                d.BaslangicTarihi, d.BitisTarihi, d.Kontenjan, d.Fiyat,
                d.Tur, d.Konum, d.OnlinePlatform, d.Durum,
                d.Kursiyerler.Count, d.DersProgramlari.Count))
            .ToListAsync();

    public async Task<EgitimDonemDto?> GetDonemAsync(int donemId) =>
        await db.EgitimDonemleri
            .Where(d => d.Id == donemId)
            .Select(d => new EgitimDonemDto(
                d.Id, d.EgitimId, d.Egitim.Ad, d.Ad,
                d.BaslangicTarihi, d.BitisTarihi, d.Kontenjan, d.Fiyat,
                d.Tur, d.Konum, d.OnlinePlatform, d.Durum,
                d.Kursiyerler.Count, d.DersProgramlari.Count))
            .FirstOrDefaultAsync();

    public async Task UpdateDonemAsync(int donemId, DonemCommand cmd)
    {
        var donem = await db.EgitimDonemleri.FindAsync(donemId) ?? throw new KeyNotFoundException();
        donem.Ad = cmd.Ad; donem.BaslangicTarihi = cmd.BaslangicTarihi; donem.BitisTarihi = cmd.BitisTarihi;
        donem.Kontenjan = cmd.Kontenjan; donem.Fiyat = cmd.Fiyat; donem.Tur = cmd.Tur;
        donem.Konum = cmd.Konum; donem.OnlinePlatform = cmd.OnlinePlatform; donem.Durum = cmd.Durum;
        await db.SaveChangesAsync();
    }

    public async Task<List<DersProgramiDto>> GetDersProgramiAsync(int donemId) =>
        await db.DersProgramlari
            .Where(d => d.DonemId == donemId)
            .OrderBy(d => d.SiraNo)
            .Select(d => new DersProgramiDto(
                d.Id, d.DonemId, d.SiraNo, d.Baslik, d.Aciklama,
                d.DersTarihi, d.SureDakika, d.IcerikMetin, d.IcerikLink, d.VideoLink,
                d.Takipler.Count(t => t.Katildi)))
            .ToListAsync();

    public async Task<DersProgramiDto> AddDersAsync(int donemId, DersProgramiCommand cmd)
    {
        var maxSira = await db.DersProgramlari.Where(d => d.DonemId == donemId).MaxAsync(d => (int?)d.SiraNo) ?? 0;
        var ders = new DersProgrami
        {
            DonemId = donemId, SiraNo = maxSira + 1, Baslik = cmd.Baslik, Aciklama = cmd.Aciklama,
            DersTarihi = cmd.DersTarihi, SureDakika = cmd.SureDakika,
            IcerikMetin = cmd.IcerikMetin, IcerikLink = cmd.IcerikLink, VideoLink = cmd.VideoLink
        };
        db.DersProgramlari.Add(ders);
        await db.SaveChangesAsync();
        return new DersProgramiDto(ders.Id, ders.DonemId, ders.SiraNo, ders.Baslik, ders.Aciklama, ders.DersTarihi, ders.SureDakika, ders.IcerikMetin, ders.IcerikLink, ders.VideoLink, 0);
    }

    public async Task UpdateDersAsync(int dersId, DersProgramiCommand cmd)
    {
        var ders = await db.DersProgramlari.FindAsync(dersId) ?? throw new KeyNotFoundException();
        ders.Baslik = cmd.Baslik; ders.Aciklama = cmd.Aciklama; ders.DersTarihi = cmd.DersTarihi;
        ders.SureDakika = cmd.SureDakika; ders.IcerikMetin = cmd.IcerikMetin;
        ders.IcerikLink = cmd.IcerikLink; ders.VideoLink = cmd.VideoLink;
        await db.SaveChangesAsync();
    }

    public async Task DeleteDersAsync(int dersId)
    {
        var ders = await db.DersProgramlari.FindAsync(dersId) ?? throw new KeyNotFoundException();
        db.DersProgramlari.Remove(ders);
        await db.SaveChangesAsync();
    }

    public async Task<List<KursiyerDto>> GetKursiyerlerAsync(int donemId)
    {
        var toplamDers = await db.DersProgramlari.CountAsync(d => d.DonemId == donemId);
        return await db.Kursiyerler
            .Where(k => k.DonemId == donemId)
            .OrderBy(k => k.Ad).ThenBy(k => k.Soyad)
            .Select(k => new KursiyerDto(
                k.Id, k.DonemId, k.Ad, k.Soyad, k.Ad + " " + k.Soyad,
                k.Email, k.Telefon, k.Sehir, k.Meslek,
                k.OdemeDurumu, k.OdenenTutar, k.KayitTarihi,
                k.SertifikaVerildi, k.SertifikaNo, k.SertifikaTarihi, k.Notlar,
                k.Takipler.Count(t => t.Katildi), toplamDers))
            .ToListAsync();
    }

    public async Task<KursiyerDto> AddKursiyerAsync(int donemId, KursiyerKayitCommand cmd)
    {
        var k = new Kursiyer
        {
            DonemId = donemId, Ad = cmd.Ad, Soyad = cmd.Soyad, Email = cmd.Email,
            Telefon = cmd.Telefon, Sehir = cmd.Sehir, Meslek = cmd.Meslek, Notlar = cmd.Notlar,
            OdemeDurumu = OdemeDurumu.Bekliyor, KayitTarihi = DateOnly.FromDateTime(DateTime.Today)
        };
        db.Kursiyerler.Add(k);
        await db.SaveChangesAsync();
        var toplamDers = await db.DersProgramlari.CountAsync(d => d.DonemId == donemId);
        return new KursiyerDto(k.Id, k.DonemId, k.Ad, k.Soyad, k.FullName, k.Email, k.Telefon, k.Sehir, k.Meslek, k.OdemeDurumu, k.OdenenTutar, k.KayitTarihi, false, null, null, k.Notlar, 0, toplamDers);
    }

    public async Task UpdateKursiyerAsync(int kursiyerId, KursiyerKayitCommand cmd)
    {
        var k = await db.Kursiyerler.FindAsync(kursiyerId) ?? throw new KeyNotFoundException();
        k.Ad = cmd.Ad; k.Soyad = cmd.Soyad; k.Email = cmd.Email;
        k.Telefon = cmd.Telefon; k.Sehir = cmd.Sehir; k.Meslek = cmd.Meslek; k.Notlar = cmd.Notlar;
        await db.SaveChangesAsync();
    }

    public async Task UpdateOdemeAsync(int kursiyerId, OdemeDurumu durum, decimal odenenTutar)
    {
        var k = await db.Kursiyerler.FindAsync(kursiyerId) ?? throw new KeyNotFoundException();
        k.OdemeDurumu = durum; k.OdenenTutar = odenenTutar;
        await db.SaveChangesAsync();
    }

    public async Task SertifikaVerAsync(int kursiyerId)
    {
        var k = await db.Kursiyerler.FindAsync(kursiyerId) ?? throw new KeyNotFoundException();
        k.SertifikaVerildi = true;
        k.SertifikaTarihi = DateOnly.FromDateTime(DateTime.Today);
        k.SertifikaNo ??= $"APARTNET-{DateTime.Today.Year}-{kursiyerId:D4}";
        await db.SaveChangesAsync();
    }

    public async Task DeleteKursiyerAsync(int kursiyerId)
    {
        var k = await db.Kursiyerler.FindAsync(kursiyerId) ?? throw new KeyNotFoundException();
        db.Kursiyerler.Remove(k);
        await db.SaveChangesAsync();
    }

    public async Task<List<DersTakibiDto>> GetTakiplerAsync(int donemId) =>
        await db.DersTakipleri
            .Where(t => t.Kursiyer.DonemId == donemId)
            .Select(t => new DersTakibiDto(t.Id, t.KursiyerId, t.Kursiyer.Ad + " " + t.Kursiyer.Soyad, t.DersProgramiId, t.DersProgrami.Baslik, t.Katildi, t.Not))
            .ToListAsync();

    public async Task SetKatilimAsync(int kursiyerId, int dersId, bool katildi)
    {
        var takip = await db.DersTakipleri.FirstOrDefaultAsync(t => t.KursiyerId == kursiyerId && t.DersProgramiId == dersId);
        if (takip is null)
        {
            db.DersTakipleri.Add(new DersTakibi { KursiyerId = kursiyerId, DersProgramiId = dersId, Katildi = katildi });
        }
        else
        {
            takip.Katildi = katildi;
        }
        await db.SaveChangesAsync();
    }
}
