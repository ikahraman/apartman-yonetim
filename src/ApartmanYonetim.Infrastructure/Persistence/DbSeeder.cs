using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApartmanYonetim.Infrastructure.Persistence;

public static class DbSeeder
{
    private static readonly string[] MonthNames = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];
    private static string PeriodLabel(int y, int m) => $"{MonthNames[m - 1]} {y}";

    private static readonly string[] MaleNames = ["Ahmet", "Mehmet", "Mustafa", "Ali", "İbrahim", "Hüseyin", "Hasan", "Murat", "Ömer", "Emre", "Burak", "Serkan", "Tolga", "Onur", "Barış", "Volkan", "Cem", "Ercan", "Kemal", "Recep", "Tamer", "Gökhan", "Ozan", "Zeki", "Hakan", "Selim", "Yusuf", "Kadir", "Fatih", "Berk", "Koray", "Umut", "Kaan", "Alp", "Arda", "Sinan", "Furkan", "Berkay", "Uğur", "Caner"];
    private static readonly string[] FemaleNames = ["Fatma", "Ayşe", "Zeynep", "Elif", "Hatice", "Merve", "Selin", "Büşra", "Derya", "Nurgül", "Canan", "Sibel", "Pınar", "Yasemin", "Rabia", "Sevda", "Duygu", "Filiz", "Şule", "Esra", "Tuğba", "Meltem", "Gamze", "Özge", "Serap", "Gül", "Meryem", "Hacer", "Zühal", "Arzu", "Berna", "Dilek", "Emel", "Gülay", "Havva", "Leyla", "Nalan", "Özlem", "Pervin", "Reyhan"];
    private static readonly string[] Surnames = ["Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Arslan", "Koç", "Öztürk", "Güneş", "Aydın", "Doğan", "Kılıç", "Çetin", "Polat", "Aksoy", "Kara", "Tan", "Eroğlu", "Bayrak", "Işık", "Şimşek", "Uzun", "Demirel", "Kartal", "Erdoğan", "Bozkurt", "Çakır", "Acar", "Toprak", "Sarı", "Özcan", "Güler", "Boz", "Kurt", "Doğru", "Keskin", "Yıldırım", "Ekici", "Aslan", "Kaplan", "Keskin", "Bulut", "Çavuş", "Dinç", "Erdem", "Güven", "Karadeniz", "Özer", "Tekin", "Yurt"];

    public static async Task SeedAsync(
        MainDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleMgr,
        FirmDbContextFactory firmFactory,
        SiteDbContextFactory siteFactory)
    {
        await db.Database.MigrateAsync();

        foreach (var role in new[] { "SuperAdmin", "Manager", "SiteStaff", "Auditor", "Resident" })
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        // SuperAdmin — firma bağımsız
        await EnsureUser(userManager, "admin@ay.com", "Admin1234!", "Sistem Yöneticisi", null, ["SuperAdmin", "Manager"]);

        if (await db.FirmRegistrations.AnyAsync()) return;

        // ── FİRMA 1: Özgür Yönetim ───────────────────────────────────────────
        var ozgurReg = await EnsureAndSeedFirm(
            db, userManager, firmFactory, siteFactory,
            slug: "ozgur-yonetim",
            name: "Özgür Yönetim A.Ş.",
            managerEmail: "ozgur@ay.com",
            managerPassword: "Ozgur1234!",
            managerDisplayName: "Özgür Kaplan",
            seedAction: async (firmDb) =>
            {
                await SeedLaleApartmani(firmDb, siteFactory);
                await SeedGunesSitesi(firmDb, siteFactory);
                await SeedYildizRezidans(firmDb, siteFactory);
            });

        // SiteStaff → lale-apartmani + gunes-sitesi (Özgür firmasına ait)
        var siteStaff = await EnsureUser(userManager, "personel@ay.com", "Personel1234!", "Test Personel", "ozgur-yonetim", ["SiteStaff"]);
        // Auditor → Özgür'ün 3 sitesi
        var auditor = await EnsureUser(userManager, "denetci@ay.com", "Denetci1234!", "Test Denetçi", "ozgur-yonetim", ["Auditor"]);

        await using (var firmDb = firmFactory.Create(ozgurReg.DbFilePath))
        {
            foreach (var slug in new[] { "lale-apartmani", "gunes-sitesi" })
            {
                var s = await firmDb.Sites.FirstOrDefaultAsync(x => x.Slug == slug);
                if (s != null && !await firmDb.UserSiteAccess.AnyAsync(u => u.UserId == siteStaff.Id && u.SiteId == s.Id))
                    firmDb.UserSiteAccess.Add(new UserSiteAccess { UserId = siteStaff.Id, SiteId = s.Id });
            }
            foreach (var slug in new[] { "lale-apartmani", "gunes-sitesi", "yildiz-rezidans" })
            {
                var s = await firmDb.Sites.FirstOrDefaultAsync(x => x.Slug == slug);
                if (s != null && !await firmDb.UserSiteAccess.AnyAsync(u => u.UserId == auditor.Id && u.SiteId == s.Id))
                    firmDb.UserSiteAccess.Add(new UserSiteAccess { UserId = auditor.Id, SiteId = s.Id });
            }
            await firmDb.SaveChangesAsync();

            // Resident → Lale A-1 ilk sakin
            var residentUser = await EnsureUser(userManager, "sakin@ay.com", "Sakin1234!", "Test Sakin", "ozgur-yonetim", ["Resident"]);
            var laleSite = await firmDb.Sites.FirstOrDefaultAsync(s => s.Slug == "lale-apartmani");
            if (laleSite != null)
            {
                await using var laleDb = siteFactory.Create(laleSite.DbFilePath);
                var firstResident = await laleDb.Residents.OrderBy(r => r.Id).FirstOrDefaultAsync();
                if (firstResident != null && firstResident.UserId is null)
                {
                    firstResident.UserId = residentUser.Id;
                    await laleDb.SaveChangesAsync();
                }
            }
        }

        // ── FİRMA 2: Sevgi Yönetim ───────────────────────────────────────────
        await EnsureAndSeedFirm(
            db, userManager, firmFactory, siteFactory,
            slug: "sevgi-yonetim",
            name: "Sevgi Yönetim A.Ş.",
            managerEmail: "yonetici@ay.com",
            managerPassword: "Yonetici1234!",
            managerDisplayName: "Sevgi Demir",
            seedAction: async (firmDb) =>
            {
                await SeedBaharSitesi(firmDb, siteFactory);
                await SeedYesilvadiKonutlari(firmDb, siteFactory);
                await SeedPrestijRezidans(firmDb, siteFactory);
                await SeedMetropolSitesi(firmDb, siteFactory);
            });
    }

    private static async Task<FirmRegistration> EnsureAndSeedFirm(
        MainDbContext db,
        UserManager<AppUser> userManager,
        FirmDbContextFactory firmFactory,
        SiteDbContextFactory siteFactory,
        string slug, string name,
        string managerEmail, string managerPassword, string managerDisplayName,
        Func<FirmDbContext, Task> seedAction)
    {
        var dbPath = firmFactory.ResolvePath(slug);
        var reg = new FirmRegistration { Name = name, Slug = slug, DbFilePath = dbPath };
        db.FirmRegistrations.Add(reg);
        await db.SaveChangesAsync();

        await using var firmDb = await firmFactory.CreateAndMigrateAsync(slug);

        if (!await firmDb.Companies.AnyAsync())
        {
            firmDb.Companies.Add(new ManagementCompany
            {
                Name = name, Slug = slug,
                Email = $"info@{slug.Replace("-", "")}.com",
            });
            await firmDb.SaveChangesAsync();
        }

        await EnsureUser(userManager, managerEmail, managerPassword, managerDisplayName, slug, ["Manager"]);

        if (!await firmDb.Sites.AnyAsync())
            await seedAction(firmDb);

        return reg;
    }

    // ─── ÖZGÜR YÖNETİM SİTELERİ ─────────────────────────────────────────────

    private static async Task SeedLaleApartmani(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "lale-apartmani";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Lale Apartmanı", Slug = slug,
            Address = "Lale Sokak No:5, Bağcılar", City = "İstanbul", UnitCount = 12, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2021, 1, 1), ContractEndDate = new DateOnly(2024, 12, 31),
            MonthlyManagementFee = 3200, ContractNotes = "2 yıllık sözleşme. Yıllık enflasyon farkı uygulanır."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        foreach (var blok in new[] { "A", "B" })
            for (int i = 1; i <= 6; i++)
                units.Add(new SiteUnit { Number = $"{blok}-{i}", Block = blok, Floor = i, SquareMeters = 85 + (i % 3) * 10 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 10, "lale", new Random(11));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 800, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.Add(schedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Lale Apartmanı Yönetimi", "lale");
        await AddMaintenance(sdb, units, new Random(11));
        await AddMeetings(sdb, "Lale Apartmanı Yönetimi", "Lale Apartmanı", 800);
        await AddAccounting(sdb, schedule, 800, 2024, 1, 2026, 5, "Lale Apartmanı Yönetimi", new Random(11));
    }

    private static async Task SeedGunesSitesi(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "gunes-sitesi";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Güneş Sitesi", Slug = slug,
            Address = "Güneş Bulvarı No:12, Çankaya", City = "Ankara", UnitCount = 24, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2022, 6, 1), ContractEndDate = new DateOnly(2025, 5, 31),
            MonthlyManagementFee = 5800, ContractNotes = "3 yıllık sözleşme. Güvenlik ve temizlik hizmetleri dahildir."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        foreach (var blok in new[] { "A", "B", "C" })
            for (int i = 1; i <= 8; i++)
                units.Add(new SiteUnit { Number = $"{blok}-{i}", Block = blok, Floor = ((i - 1) / 2) + 1, SquareMeters = 90 + (i % 4) * 15 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 20, "gunes", new Random(22));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 1200, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.Add(schedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Güneş Sitesi Yönetimi", "gunes");
        await AddMaintenance(sdb, units, new Random(22));
        await AddMeetings(sdb, "Güneş Sitesi Yönetimi", "Güneş Sitesi", 1200);
        await AddAccounting(sdb, schedule, 1200, 2024, 1, 2026, 5, "Güneş Sitesi Yönetimi", new Random(22));
    }

    private static async Task SeedYildizRezidans(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "yildiz-rezidans";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Yıldız Rezidans", Slug = slug,
            Address = "Yıldız Caddesi No:33, Konak", City = "İzmir", UnitCount = 18, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2023, 3, 1), ContractEndDate = new DateOnly(2026, 2, 28),
            MonthlyManagementFee = 4500, ContractNotes = "3 yıllık sözleşme. Peyzaj ve havuz bakımı yönetim firmasınca karşılanır."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        for (int i = 1; i <= 18; i++)
            units.Add(new SiteUnit { Number = $"{i}", Block = null, Floor = ((i - 1) / 3) + 1, SquareMeters = 100 + (i % 5) * 20 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 15, "yildiz", new Random(33));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 1500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.Add(schedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Yıldız Rezidans Yönetimi", "yildiz");
        await AddMaintenance(sdb, units, new Random(33));
        await AddMeetings(sdb, "Yıldız Rezidans Yönetimi", "Yıldız Rezidans", 1500);
        await AddAccounting(sdb, schedule, 1500, 2024, 1, 2026, 5, "Yıldız Rezidans Yönetimi", new Random(33));
    }

    // ─── SEVGİ YÖNETİM SİTELERİ ─────────────────────────────────────────────

    private static async Task SeedBaharSitesi(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "bahar-sitesi";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Bahar Sitesi", Slug = slug,
            Address = "Bahar Sokak No:8, Kadıköy", City = "İstanbul", UnitCount = 36, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2023, 1, 1), ContractEndDate = new DateOnly(2025, 12, 31),
            MonthlyManagementFee = 8500, ContractNotes = "2 yıllık yönetim sözleşmesi. Yıllık TÜFE+5 artış hakkı mevcuttur."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        foreach (var blok in new[] { "A", "B", "C" })
            for (int i = 1; i <= 12; i++)
                units.Add(new SiteUnit { Number = $"{blok}-{i}", Block = blok, Floor = ((i - 1) / 2) + 1, SquareMeters = 85 + (i % 4) * 15 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 29, "bahar", new Random(101));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 1500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.Add(schedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Bahar Sitesi Yönetimi", "bahar");
        await AddMaintenance(sdb, units, new Random(101));
        await AddMeetings(sdb, "Bahar Sitesi Yönetimi", "Bahar Sitesi", 1500);
        await AddAccounting(sdb, schedule, 1500, 2024, 1, 2026, 5, "Bahar Sitesi Yönetimi", new Random(101));
    }

    private static async Task SeedYesilvadiKonutlari(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "yesilvadi-konutlari";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Yeşilvadi Konutları", Slug = slug,
            Address = "Yeşilvadi Bulvarı No:45, Çankaya", City = "Ankara", UnitCount = 36, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2024, 3, 1), ContractEndDate = new DateOnly(2027, 2, 28),
            MonthlyManagementFee = 9200, ContractNotes = "3 yıllık sözleşme. Teknik ekip haftada 2 gün sahada görev yapar."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        foreach (var blok in new[] { "A", "B", "C", "D" })
            for (int i = 1; i <= 9; i++)
                units.Add(new SiteUnit { Number = $"{blok}-{i}", Block = blok, Floor = ((i - 1) / 3) + 1, SquareMeters = 90 + (i % 3) * 20 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 29, "yesilvadi", new Random(202));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 1800, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.Add(schedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Yeşilvadi Konutları Yönetimi", "yesilvadi");
        await AddMaintenance(sdb, units, new Random(202));
        await AddMeetings(sdb, "Yeşilvadi Konutları Yönetimi", "Yeşilvadi Konutları", 1800);
        await AddAccounting(sdb, schedule, 1800, 2024, 1, 2026, 5, "Yeşilvadi Konutları Yönetimi", new Random(202));
    }

    private static async Task SeedPrestijRezidans(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "prestij-rezidans";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Prestij Rezidans", Slug = slug,
            Address = "Prestij Caddesi No:200, Ataşehir", City = "İstanbul", UnitCount = 80, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2022, 6, 1), ContractEndDate = new DateOnly(2026, 5, 31),
            MonthlyManagementFee = 28000, ContractNotes = "4 yıllık premium yönetim sözleşmesi. 7/24 güvenlik dahildir."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        foreach (var blok in new[] { "A", "B", "C", "D" })
            for (int i = 1; i <= 20; i++)
                units.Add(new SiteUnit { Number = $"{blok}-{i}", Block = blok, Floor = ((i - 1) / 2) + 1, SquareMeters = 110 + (i % 5) * 25 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 64, "prestij", new Random(303));
        var mainSchedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 3000, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        var otoparkSchedule = new SiteFeeSchedule { Name = "Otopark & Havuz", Amount = 800, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.AddRange(mainSchedule, otoparkSchedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, mainSchedule, 2024, 1, 2026, 5);
        await GeneratePayments(sdb, otoparkSchedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Prestij Rezidans Yönetimi", "prestij");
        await AddMaintenance(sdb, units, new Random(303));
        await AddMeetings(sdb, "Prestij Rezidans Yönetimi", "Prestij Rezidans", 3000);
        await AddAccounting(sdb, mainSchedule, 3000, 2024, 1, 2026, 5, "Prestij Rezidans Yönetimi", new Random(303));
    }

    private static async Task SeedMetropolSitesi(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "metropol-sitesi";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var company = await firmDb.Companies.FirstAsync();
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = "Metropol Sitesi", Slug = slug,
            Address = "Atatürk Caddesi No:310, Alsancak", City = "İzmir", UnitCount = 96, DbFilePath = dbPath,
            ContractStartDate = new DateOnly(2023, 9, 1), ContractEndDate = new DateOnly(2026, 8, 31),
            MonthlyManagementFee = 22500, ContractNotes = "3 yıllık sözleşme. Sosyal tesis yönetimi dahildir."
        };
        firmDb.Sites.Add(site); await firmDb.SaveChangesAsync();
        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;
        var units = new List<SiteUnit>();
        foreach (var blok in new[] { "A", "B", "C", "D", "E", "F" })
            for (int i = 1; i <= 16; i++)
                units.Add(new SiteUnit { Number = $"{blok}-{i}", Block = blok, Floor = ((i - 1) / 2) + 1, SquareMeters = 95 + (i % 6) * 20 });
        sdb.Units.AddRange(units); await sdb.SaveChangesAsync();
        await AddGeneratedResidents(sdb, units, 77, "metropol", new Random(404));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 2500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        var spSchedule = new SiteFeeSchedule { Name = "Sosyal Tesis Aidatı", Amount = 500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1) };
        sdb.FeeSchedules.AddRange(schedule, spSchedule); await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, 2024, 1, 2026, 5);
        await GeneratePayments(sdb, spSchedule, 2024, 1, 2026, 5);
        await AddAnnouncements(sdb, "Metropol Sitesi Yönetimi", "metropol");
        await AddMaintenance(sdb, units, new Random(404));
        await AddMeetings(sdb, "Metropol Sitesi Yönetimi", "Metropol Sitesi", 2500);
        await AddAccounting(sdb, schedule, 2500, 2024, 1, 2026, 5, "Metropol Sitesi Yönetimi", new Random(404));
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static async Task<AppUser> EnsureUser(UserManager<AppUser> um, string email, string password,
        string displayName, string? firmSlug, string[] roles)
    {
        var user = await um.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email, Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                FirmSlug = firmSlug
            };
            await um.CreateAsync(user, password);
        }
        else if (user.FirmSlug != firmSlug)
        {
            user.FirmSlug = firmSlug;
            await um.UpdateAsync(user);
        }
        foreach (var role in roles)
            if (!await um.IsInRoleAsync(user, role))
                await um.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task AddGeneratedResidents(SiteDbContext db, List<SiteUnit> units, int count, string sitePrefix, Random rnd)
    {
        var types = new[] { ResidencyType.Owner, ResidencyType.Owner, ResidencyType.Tenant, ResidencyType.Tenant, ResidencyType.Family };
        var moveInYears = new[] { 2019, 2020, 2021, 2022, 2023, 2024 };

        for (int i = 0; i < count && i < units.Count; i++)
        {
            var unit = units[i];
            var isMale = rnd.Next(2) == 0;
            var firstName = isMale ? MaleNames[rnd.Next(MaleNames.Length)] : FemaleNames[rnd.Next(FemaleNames.Length)];
            var lastName = Surnames[rnd.Next(Surnames.Length)];
            var type = types[rnd.Next(types.Length)];
            var phone = $"05{rnd.Next(30, 40)} {rnd.Next(100, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}";
            var email = $"{sitePrefix}.{unit.Number.ToLower().Replace("-", "")}@ornek.com";
            var moveIn = new DateOnly(moveInYears[rnd.Next(moveInYears.Length)], rnd.Next(1, 13), 1);

            db.Residents.Add(new SiteResident
            {
                UnitId = unit.Id, FirstName = firstName, LastName = lastName,
                Phone = phone, Email = email, ResidencyType = type, MoveInDate = moveIn
            });
            unit.OccupancyType = type == ResidencyType.Owner ? OccupancyType.Owner : OccupancyType.Tenant;
        }
        await db.SaveChangesAsync();
    }

    private static async Task GeneratePayments(SiteDbContext db, SiteFeeSchedule schedule,
        int fromYear, int fromMonth, int toYear, int toMonth)
    {
        var units = await db.Units.ToListAsync();
        var rnd = new Random(42);
        var (cy, cm) = (fromYear, fromMonth);
        while (cy < toYear || (cy == toYear && cm <= toMonth))
        {
            var label = PeriodLabel(cy, cm);
            var dueDate = new DateOnly(cy, cm, Math.Min(schedule.DueDay, DateTime.DaysInMonth(cy, cm)));
            foreach (var unit in units)
            {
                var isPast = new DateOnly(cy, cm, 1) < new DateOnly(2026, 5, 1);
                var paid = isPast && rnd.NextDouble() > 0.18;
                db.FeePayments.Add(new SiteFeePayment
                {
                    UnitId = unit.Id, ScheduleId = schedule.Id,
                    PeriodLabel = label, DueDate = dueDate, Amount = schedule.Amount,
                    Status = paid ? FeePaymentStatus.Paid : (isPast ? FeePaymentStatus.Overdue : FeePaymentStatus.Pending),
                    PaidDate = paid ? dueDate.AddDays(rnd.Next(0, 12)) : null,
                    PaidAmount = paid ? schedule.Amount : null
                });
            }
            if (cm == 12) { cy++; cm = 1; } else cm++;
        }
        await db.SaveChangesAsync();
    }

    private static async Task AddAnnouncements(SiteDbContext db, string author, string prefix)
    {
        var now = DateTime.Now;
        var items = new[]
        {
            ("Asansör Periyodik Bakımı", $"Binamızın asansörü periyodik yıllık bakıma alınacaktır. Bakım süresince lütfen merdivenleri kullanınız.", false, now.AddDays(-200)),
            ("Su Kesintisi Duyurusu", "Yarın 08:00-17:00 saatleri arasında ana hat bakımı nedeniyle su kesintisi yaşanacaktır.", true, now.AddDays(-155)),
            ("Ortak Alan Temizlik Takvimi", "Ortak alanlar her Salı ve Cuma 09:00-13:00 arasında temizlenmektedir.", false, now.AddDays(-130)),
            ("2024 Olağan Genel Kurul", "2024 yılı genel kurul toplantımız 16 Mart 2024 Cumartesi saat 14:00'te yapılacaktır.", false, now.AddDays(-100)),
            ("Güvenlik Sistemi Güncellendi", "Ortak alanlara yeni nesil IP kameralar kurulmuş, 7/24 kayıt sistemi devreye girmiştir.", false, now.AddDays(-80)),
            ("Bahçe ve Peyzaj Çalışması", "1-7 Mayıs tarihleri arasında bahçe peyzaj yenilemesi yapılacaktır.", false, now.AddDays(-65)),
            ("Elektrik Paneli Bakımı", "Ana elektrik panosunun bakımı 22 Nisan'da gerçekleştirilecek.", true, now.AddDays(-50)),
            ("Dış Cephe Boyama Çalışması", "Haziran ayı boyunca bina dış cephesi yenilenecektir.", false, now.AddDays(-35)),
            ("Otopark Numaralandırma Sistemi", "Otopark yerleri yeniden numaralandırılmıştır. Araç plaka bilgilerinizi iletiniz.", false, now.AddDays(-22)),
            ("Isı Yalıtım Projesi Başlıyor", "Enerji verimliliği kapsamında bina dış cephesine ısı yalıtımı uygulanacaktır.", false, now.AddDays(-10)),
            ("Doğalgaz Sayaç Değişimi", "15-20 Haziran tarihleri arasında sayaç değişimi yapılacaktır. Evde bulunmanız gerekmektedir.", true, now.AddDays(-5)),
            ("Çocuk Parkı Yenilendi", "Site içindeki çocuk oyun alanı komple yenilendi. Ailelere hayırlı olsun!", false, now.AddDays(-2)),
        };

        foreach (var (title, content, urgent, date) in items)
            db.Announcements.Add(new SiteAnnouncement
            {
                Title = title, Content = content, IsUrgent = urgent,
                PublishedAt = date, CreatedBy = author,
                ExpiresAt = urgent ? date.AddDays(30) : null
            });
        await db.SaveChangesAsync();
    }

    private static async Task AddMaintenance(SiteDbContext db, List<SiteUnit> units, Random rnd)
    {
        var now = DateTime.Now;
        var items = new[]
        {
            ("Elektrik sigortası atıyor", "Daire içi elektrik sigortası sürekli düşüyor.", "Elektrik", MaintenancePriority.High, MaintenanceStatus.Resolved, "Sigorta kutusu ve devre kesici yenilendi."),
            ("Mutfakta su sızıntısı", "Mutfak dolabının altından su geliyor.", "Su/Tesisat", MaintenancePriority.High, MaintenanceStatus.Resolved, "Mutfak altı boru bağlantısı yenilendi."),
            ("Asansör arızası", "Asansör 4. katta kapısı açmıyor.", "Asansör", MaintenancePriority.Urgent, MaintenanceStatus.Resolved, "Kapı mekanizması değiştirildi."),
            ("Koridor lambası arızalı", "3. kat koridoru tamamen karanlık.", "Ortak Alan", MaintenancePriority.Normal, MaintenanceStatus.Resolved, "LED armatür değiştirildi."),
            ("Giriş kapısı kilidi bozuk", "Bina ana giriş kapısı kilidi bazen açılmıyor.", "Güvenlik", MaintenancePriority.High, MaintenanceStatus.InProgress, null),
            ("Doğalgaz kokusu şikâyeti", "Bodrum katta gaz kokusu var.", "Isıtma/Soğutma", MaintenancePriority.Urgent, MaintenanceStatus.Resolved, "Boru bağlantısında küçük sızıntı giderildi."),
            ("Yağmur oluğu tıkanıklığı", "Çatı yağmur olukları tıkalı.", "Çatı/Dış Cephe", MaintenancePriority.Normal, MaintenanceStatus.Open, null),
            ("Otopark aydınlatma arızası", "Otopark girişi tamamen karanlık.", "Elektrik", MaintenancePriority.Normal, MaintenanceStatus.InProgress, null),
            ("Giriş zemini kaygan", "Islak havalarda giriş mermeri çok kayıyor.", "Ortak Alan", MaintenancePriority.High, MaintenanceStatus.Resolved, "Kaymaz bant uygulandı."),
            ("Kalorifer gürültüsü", "Kalorifer borularından tıklama sesi geliyor.", "Isıtma/Soğutma", MaintenancePriority.Low, MaintenanceStatus.Open, null),
            ("Çatı su sızıntısı", "Son yağışlarda en üst kattaki tavanda sızıntı oluştu.", "Çatı/Dış Cephe", MaintenancePriority.High, MaintenanceStatus.InProgress, null),
            ("Havuz filtre arızası", "Havuz filtre sistemi çalışmıyor.", "Ortak Alan", MaintenancePriority.Normal, MaintenanceStatus.Resolved, "Filtre pompası değiştirildi."),
            ("Yangın tüpü süresi dolmuş", "B blok yangın tüplerinin son kullanma tarihi geçmiş.", "Güvenlik", MaintenancePriority.High, MaintenanceStatus.Resolved, "Tüm blokların yangın tüpleri yenilendi."),
            ("Bahçe sulama sistemi arızası", "Otomatik sulama sistemi çalışmıyor.", "Ortak Alan", MaintenancePriority.Low, MaintenanceStatus.Open, null),
            ("Bodrum nem sorunu", "Bodrum katında aşırı nem var.", "Su/Tesisat", MaintenancePriority.Normal, MaintenanceStatus.InProgress, null),
        };

        for (int i = 0; i < items.Length; i++)
        {
            var (title, desc, cat, priority, status, resolution) = items[i];
            var unit = units.Count > i ? units[rnd.Next(units.Count)] : null;
            db.MaintenanceRequests.Add(new SiteMaintenanceRequest
            {
                UnitId = status != MaintenanceStatus.Open ? unit?.Id : null,
                ReporterName = status == MaintenanceStatus.Open ? "Sakin Bildirimi" : "Site Yönetimi",
                Title = title, Description = desc, Category = cat,
                Priority = priority, Status = status,
                ReportedAt = DateTime.Now.AddDays(-rnd.Next(10, 200)),
                AssignedTo = status != MaintenanceStatus.Open ? "Bakım & Teknik Ekibi" : null,
                ResolvedAt = status == MaintenanceStatus.Resolved ? DateTime.Now.AddDays(-rnd.Next(1, 15)) : null,
                ResolutionNotes = resolution
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task AddMeetings(SiteDbContext db, string author, string siteName, decimal aidat)
    {
        var now = DateTime.Now;
        var newAidat = (int)(aidat * 1.15m / 100) * 100;
        var meetings = new[]
        {
            ($"{siteName} 2024 Olağan Genel Kurulu", MeetingType.Ordinary, MeetingStatus.Completed,
             new DateTime(2024, 3, 16, 14, 0, 0), "Toplantı Salonu - Zemin Kat",
             "1. Açılış ve yoklama\n2. Divan seçimi\n3. 2023 faaliyet raporu\n4. Denetim kurulu raporu\n5. Bütçe onayı\n6. Aidat belirlenmesi",
             $"2023 yılı hesapları incelendi, yönetim ibra edildi. 2024 aylık aidat {aidat:N0} TL olarak belirlendi.",
             $"Aidat {aidat:N0} TL, yönetim ibra edildi.", 25 + (int)(aidat / 200)),

            ("Acil Toplantı – Asansör Yenileme", MeetingType.Emergency, MeetingStatus.Completed,
             new DateTime(2024, 5, 10, 19, 0, 0), "Toplantı Salonu",
             "1. Asansör arızaları ve yenileme teklifleri\n2. Finansman seçenekleri\n3. Firma seçimi",
             "Üç firmadan teklif değerlendirildi. En uygun firma seçildi.",
             "Asansör yenileme anlaşması yapıldı.", 18 + (int)(aidat / 300)),

            ($"2024 Yıl Sonu ve 2025 Bütçe Toplantısı", MeetingType.Ordinary, MeetingStatus.Completed,
             new DateTime(2024, 12, 14, 15, 0, 0), "Toplantı Salonu",
             "1. 2024 yıl değerlendirmesi\n2. Gelir-gider dengesi\n3. 2025 tahmini bütçe\n4. Aidat revizyonu",
             $"2024 yılı başarılı geçti. Enflasyon göz önünde bulundurularak aidat artışı görüşüldü.",
             $"2025 aidatı %15 artışla {newAidat:N0} TL oldu.", 28 + (int)(aidat / 250)),

            ($"{siteName} 2025 Olağan Genel Kurulu", MeetingType.Ordinary, MeetingStatus.Completed,
             new DateTime(2025, 3, 22, 14, 0, 0), "Toplantı Salonu",
             "1. Açılış ve divan seçimi\n2. 2024 faaliyet raporu\n3. Denetim raporu ve ibra\n4. 2025 bütçe onayı",
             "2024 yılı hesapları detaylıca incelendi. Yönetim kurulu ibra edildi.",
             "Yönetim kurulu yenilendi, 2025 bütçesi onaylandı.", 30 + (int)(aidat / 200)),

            ("2025 Kış Hazırlıkları ve Isıtma Sistemi", MeetingType.Ordinary, MeetingStatus.Scheduled,
             now.AddDays(12).Date.AddHours(19), "Toplantı Salonu",
             "1. Isıtma sistemi yıllık bakımı\n2. Kış bütçesi\n3. Boru ve yalıtım kontrolü", null, null, 0),

            ($"{siteName} 2026 Genel Kurul Hazırlığı", MeetingType.Ordinary, MeetingStatus.Scheduled,
             now.AddDays(35).Date.AddHours(14), "Toplantı Salonu",
             "1. 2025 yılı faaliyet raporu\n2. 2026 bütçe taslağı\n3. Aidat revizyonu", null, null, 0),
        };

        foreach (var (title, type, status, date, location, agenda, minutesContent, decisions, attendees) in meetings)
        {
            var meeting = new SiteMeeting
            {
                Title = title, MeetingType = type, Status = status,
                MeetingDate = date, Location = location, AgendaItems = agenda, CreatedBy = author
            };
            db.Meetings.Add(meeting);
            await db.SaveChangesAsync();

            if (status == MeetingStatus.Completed && minutesContent is not null)
            {
                db.MeetingMinutes.Add(new SiteMeetingMinutes
                {
                    MeetingId = meeting.Id, Content = minutesContent,
                    AttendeeCount = attendees, Decisions = decisions
                });
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task AddAccounting(SiteDbContext db, SiteFeeSchedule schedule,
        decimal aidat, int fromYear, int fromMonth, int toYear, int toMonth, string author, Random rnd)
    {
        var (cy, cm) = (fromYear, fromMonth);
        while (cy < toYear || (cy == toYear && cm <= toMonth))
        {
            var date = new DateOnly(cy, cm, 1);
            var unitCount = await db.Units.CountAsync();

            db.AccountingEntries.Add(new SiteAccountingEntry
            {
                Type = AccountingEntryType.Income, Category = "Aidat Geliri",
                Amount = aidat * unitCount * (decimal)(0.78 + rnd.NextDouble() * 0.15),
                Date = date.AddDays(15), Description = $"{PeriodLabel(cy, cm)} aidat tahsilatı", CreatedBy = author
            });

            decimal elec = 800 + aidat / 10 + rnd.Next(-100, 300);
            decimal water = 400 + aidat / 20 + rnd.Next(-50, 150);
            decimal clean = 600 + aidat / 8 + rnd.Next(-50, 100);
            decimal security = 2000 + aidat / 5 + rnd.Next(-100, 300);
            decimal mgmt = aidat / 6;

            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Elektrik", Amount = elec, Date = date.AddDays(5), Description = $"{PeriodLabel(cy, cm)} elektrik faturası", CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Su", Amount = water, Date = date.AddDays(8), Description = $"{PeriodLabel(cy, cm)} su faturası", CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Temizlik Hizmeti", Amount = clean, Date = date.AddDays(1), Description = $"{PeriodLabel(cy, cm)} temizlik hizmeti", CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Güvenlik", Amount = security, Date = date.AddDays(1), Description = $"{PeriodLabel(cy, cm)} güvenlik hizmeti", CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Yönetim Ücreti", Amount = mgmt, Date = date.AddDays(1), Description = $"{PeriodLabel(cy, cm)} yönetim ücreti", CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Asansör Bakımı", Amount = 300 + rnd.Next(0, 200), Date = date.AddDays(10), Description = $"{PeriodLabel(cy, cm)} asansör bakım sözleşmesi", CreatedBy = author });

            if (cm is >= 10 or <= 4)
                db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Doğalgaz", Amount = 1200 + aidat / 8 + rnd.Next(-200, 600), Date = date.AddDays(12), Description = $"{PeriodLabel(cy, cm)} doğalgaz faturası", CreatedBy = author });

            if (rnd.NextDouble() > 0.55)
            {
                var cats = new[] { "Bakım-Onarım", "Sigorta", "Peyzaj", "Kırtasiye", "Hukuki Gider", "Banka Komisyonu" };
                var cat = cats[rnd.Next(cats.Length)];
                db.AccountingEntries.Add(new SiteAccountingEntry
                {
                    Type = AccountingEntryType.Expense, Category = cat,
                    Amount = 200 + rnd.Next(100, (int)(aidat / 2)),
                    Date = date.AddDays(rnd.Next(2, 28)),
                    Description = $"{PeriodLabel(cy, cm)} {cat.ToLowerInvariant()} gideri", CreatedBy = author
                });
            }

            if (rnd.NextDouble() > 0.7)
            {
                db.AccountingEntries.Add(new SiteAccountingEntry
                {
                    Type = AccountingEntryType.Income, Category = "Diğer Gelir",
                    Amount = 500 + rnd.Next(200, 2000),
                    Date = date.AddDays(rnd.Next(5, 25)),
                    Description = $"{PeriodLabel(cy, cm)} otopark / reklam panosu geliri", CreatedBy = author
                });
            }

            if (cm == 12) { cy++; cm = 1; } else cm++;
        }
        await db.SaveChangesAsync();
    }
}
