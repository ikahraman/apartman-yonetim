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

    private static readonly string[] MaleNames = ["Ahmet", "Mehmet", "Mustafa", "Ali", "İbrahim", "Hüseyin", "Hasan", "Murat", "Ömer", "Emre", "Burak", "Serkan", "Tolga", "Onur", "Barış", "Volkan", "Cem", "Ercan", "Kemal", "Recep", "Tamer", "Gökhan", "Ozan", "Zeki", "Hakan", "Selim", "Yusuf", "Kadir", "Fatih", "Berk"];
    private static readonly string[] FemaleNames = ["Fatma", "Ayşe", "Zeynep", "Elif", "Hatice", "Merve", "Selin", "Büşra", "Derya", "Nurgül", "Canan", "Sibel", "Pınar", "Yasemin", "Rabia", "Sevda", "Duygu", "Filiz", "Şule", "Esra", "Tuğba", "Meltem", "Gamze", "Özge", "Serap", "Gül", "Meryem", "Hacer", "Zühal", "Arzu"];
    private static readonly string[] Surnames = ["Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Arslan", "Koç", "Öztürk", "Güneş", "Aydın", "Doğan", "Kılıç", "Çetin", "Polat", "Aksoy", "Kara", "Tan", "Eroğlu", "Bayrak", "Işık", "Şimşek", "Uzun", "Demirel", "Kartal", "Erdoğan", "Bozkurt", "Çakır", "Acar", "Toprak", "Sarı", "Özcan", "Güler", "Boz", "Kurt", "Doğru", "Keskin", "Yıldırım", "Ekici", "Aslan", "Kaplan"];

    public static async Task SeedAsync(
        MainDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleMgr,
        FirmDbContextFactory firmFactory,
        SiteDbContextFactory siteFactory)
    {
        await db.Database.MigrateAsync();

        foreach (var role in new[] { "SuperAdmin", "Manager", "SiteManager", "Auditor", "Accountant", "Resident", "EgitimAdmin" })
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        // SiteStaff → SiteManager migration (eski rol adı)
        var oldRole = await roleMgr.FindByNameAsync("SiteStaff");
        if (oldRole != null)
        {
            var usersInOldRole = await userManager.GetUsersInRoleAsync("SiteStaff");
            foreach (var u in usersInOldRole)
            {
                await userManager.RemoveFromRoleAsync(u, "SiteStaff");
                if (!await userManager.IsInRoleAsync(u, "SiteManager"))
                    await userManager.AddToRoleAsync(u, "SiteManager");
            }
            await roleMgr.DeleteAsync(oldRole);
        }

        await EnsureUser(userManager, "admin@ay.com", "Admin1234!", "Sistem Yöneticisi", null, ["SuperAdmin"]);
        // Manager rolü SuperAdmin'e verilmemeli — varsa temizle
        var adminUser = await userManager.FindByEmailAsync("admin@ay.com");
        if (adminUser != null && await userManager.IsInRoleAsync(adminUser, "Manager"))
            await userManager.RemoveFromRoleAsync(adminUser, "Manager");

        await SeedPackages(db);
        await SeedBillingConfig(db);

        if (await db.FirmRegistrations.AnyAsync())
        {
            await MigrateExistingFirmDbsAsync(db, firmFactory);
            await SeedMissingSubscriptions(db);
            await SeedMissingObligations(db, firmFactory);
            await SeedMissingStaffDataAsync(db, userManager, firmFactory);
            await SeedEgitimAsync(db, userManager);
            return;
        }

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
            });

        var siteStaff = await EnsureUser(userManager, "personel@ay.com", "Personel1234!", "Test Personel", "ozgur-yonetim", ["SiteManager"]);
        var auditor   = await EnsureUser(userManager, "denetci@ay.com",  "Denetci1234!",  "Test Denetçi",  "ozgur-yonetim", ["Auditor"]);

        await using (var firmDb = firmFactory.Create(ozgurReg.DbFilePath))
        {
            var company = await firmDb.Companies.FirstAsync();

            // CompanyStaff kayıtları (yoksa oluştur)
            if (!await firmDb.CompanyStaff.AnyAsync(s => s.UserId == siteStaff.Id))
                firmDb.CompanyStaff.Add(new CompanyStaff { CompanyId = company.Id, UserId = siteStaff.Id, Role = StaffRole.SiteManager, IsActive = true });
            if (!await firmDb.CompanyStaff.AnyAsync(s => s.UserId == auditor.Id))
                firmDb.CompanyStaff.Add(new CompanyStaff { CompanyId = company.Id, UserId = auditor.Id, Role = StaffRole.Auditor, IsActive = true });
            await firmDb.SaveChangesAsync();

            var staffRecord   = await firmDb.CompanyStaff.FirstAsync(s => s.UserId == siteStaff.Id);
            var auditorRecord = await firmDb.CompanyStaff.FirstAsync(s => s.UserId == auditor.Id);

            foreach (var slug in new[] { "lale-apartmani", "gunes-sitesi" })
            {
                var s = await firmDb.Sites.FirstOrDefaultAsync(x => x.Slug == slug);
                if (s is null) continue;
                foreach (var staffEntry in new[] { staffRecord, auditorRecord })
                {
                    if (!await firmDb.SiteStaffAssignments.AnyAsync(a => a.StaffId == staffEntry.Id && a.SiteId == s.Id && a.RemovedAt == null))
                        firmDb.SiteStaffAssignments.Add(new SiteStaffAssignment { StaffId = staffEntry.Id, SiteId = s.Id });
                }
            }
            await firmDb.SaveChangesAsync();

            var residentUser = await EnsureUser(userManager, "sakin@ay.com", "Sakin1234!", "Test Sakin", "ozgur-yonetim", ["Resident"]);
            var laleSite = await firmDb.Sites.FirstOrDefaultAsync(s => s.Slug == "lale-apartmani");
            if (laleSite is not null)
            {
                if (residentUser.SiteId != laleSite.Id)
                {
                    residentUser.SiteId = laleSite.Id;
                    await userManager.UpdateAsync(residentUser);
                }
                await using var laleDb = siteFactory.Create(laleSite.DbFilePath);
                var firstResident = await laleDb.Residents.OrderBy(r => r.Id).FirstOrDefaultAsync();
                if (firstResident is not null && firstResident.UserId is null)
                {
                    firstResident.UserId = residentUser.Id;
                    await laleDb.SaveChangesAsync();
                }
            }

            var residentUser2 = await EnsureUser(userManager, "sakin2@ay.com", "Sakin21234!", "Test Sakin 2", "ozgur-yonetim", ["Resident"]);
            var gunesSite = await firmDb.Sites.FirstOrDefaultAsync(s => s.Slug == "gunes-sitesi");
            if (gunesSite is not null)
            {
                if (residentUser2.SiteId != gunesSite.Id)
                {
                    residentUser2.SiteId = gunesSite.Id;
                    await userManager.UpdateAsync(residentUser2);
                }
                await using var gunesDb = siteFactory.Create(gunesSite.DbFilePath);
                var firstResident2 = await gunesDb.Residents.OrderBy(r => r.Id).FirstOrDefaultAsync();
                if (firstResident2 is not null && firstResident2.UserId is null)
                {
                    firstResident2.UserId = residentUser2.Id;
                    await gunesDb.SaveChangesAsync();
                }
            }
        }

        // Özgür Yönetim aboneliği — Küçük paketi (2-5 site), 2 sitesi var
        var kucukPkg = await db.FirmPackages.FirstAsync(p => p.Name == "Küçük");
        if (!await db.FirmSubscriptions.AnyAsync(s => s.FirmSlug == "ozgur-yonetim"))
        {
            db.FirmSubscriptions.Add(new FirmSubscription
            {
                FirmSlug = "ozgur-yonetim", FirmPackageId = kucukPkg.Id,
                ContractStartDate = new DateOnly(2025, 1, 1), ContractEndDate = new DateOnly(2025, 12, 31),
                Status = SubscriptionStatus.Active, LastModifiedBy = "admin@ay.com", LastModifiedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await SeedPaymentHistory(db, "ozgur-yonetim", kucukPkg.MonthlyPrice, 2025, 1, 2026, 6);
        }

        // ── FİRMA 2: Sevgi Yönetim ───────────────────────────────────────────
        await EnsureAndSeedFirm(
            db, userManager, firmFactory, siteFactory,
            slug: "sevgi-yonetim",
            name: "Sevgi Yönetim A.Ş.",
            managerEmail: "sevgi@ay.com",
            managerPassword: "Sevgi1234!",
            managerDisplayName: "Sevgi Demir",
            seedAction: async (firmDb) =>
            {
                await SeedBaharSitesi(firmDb, siteFactory);
                await SeedPrestijTopluYapi(firmDb, siteFactory);
            });

        // Sevgi Yönetim aboneliği — Küçük paketi, 2 sitesi var
        if (!await db.FirmSubscriptions.AnyAsync(s => s.FirmSlug == "sevgi-yonetim"))
        {
            db.FirmSubscriptions.Add(new FirmSubscription
            {
                FirmSlug = "sevgi-yonetim", FirmPackageId = kucukPkg.Id,
                ContractStartDate = new DateOnly(2024, 6, 1), ContractEndDate = new DateOnly(2025, 5, 31),
                Status = SubscriptionStatus.Overdue,
                Notes = "Sözleşme yenileme görüşmeleri devam ediyor.",
                LastModifiedBy = "admin@ay.com", LastModifiedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await SeedPaymentHistory(db, "sevgi-yonetim", kucukPkg.MonthlyPrice, 2024, 6, 2026, 6, overdueFromMonth: (2026, 4));
        }

        await SeedMissingObligations(db, firmFactory);
        await SeedEgitimAsync(db, userManager);
    }

    private static async Task<FirmRegistration> EnsureAndSeedFirm(
        MainDbContext db, UserManager<AppUser> userManager,
        FirmDbContextFactory firmFactory, SiteDbContextFactory siteFactory,
        string slug, string name, string managerEmail, string managerPassword,
        string managerDisplayName, Func<FirmDbContext, Task> seedAction)
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
                ContactPerson = managerDisplayName
            });
            await firmDb.SaveChangesAsync();
        }
        await EnsureUser(userManager, managerEmail, managerPassword, managerDisplayName, slug, ["Manager"]);
        if (!await firmDb.Sites.AnyAsync())
            await seedAction(firmDb);
        return reg;
    }

    // ─── ÖZGÜR YÖNETİM ───────────────────────────────────────────────────────

    private static async Task SeedLaleApartmani(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "lale-apartmani";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var laleCompanyId = (await firmDb.Companies.FirstAsync()).Id;
        var laleSiteProfile = new SiteProfile
        {
            CompanyId = laleCompanyId,
            Name = "Lale Apartmanı", Slug = slug, SiteType = SiteType.Apartman,
            Address = "Lale Sokak No:5, Bağcılar", City = "İstanbul", UnitCount = 12, DbFilePath = dbPath
        };
        firmDb.Sites.Add(laleSiteProfile);
        await firmDb.SaveChangesAsync();
        firmDb.SiteContracts.Add(new SiteContract
        {
            SiteId = laleSiteProfile.Id, StartDate = new DateOnly(2021, 1, 1), EndDate = new DateOnly(2024, 12, 31),
            MonthlyFee = 3200, Notes = "2 yıllık sözleşme. Yıllık enflasyon farkı uygulanır.",
            Status = ContractStatus.Active, Scope = ContractScope.Tumu, FeeType = ManagementFeeType.Fixed
        });
        await firmDb.SaveChangesAsync();

        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;

        // Bloklar
        var blokA = new SiteBlock { Name = "A Blok", Code = "A", FloorCount = 3, UnitCount = 6 };
        var blokB = new SiteBlock { Name = "B Blok", Code = "B", FloorCount = 3, UnitCount = 6 };
        sdb.Blocks.AddRange(blokA, blokB);
        await sdb.SaveChangesAsync();

        // Arsa payları (toplam 120 pay, her daire 10 pay — küçük farklılıklar)
        decimal[] arsaPayiA = [9m, 9m, 10m, 10m, 11m, 11m];
        decimal[] arsaPayiB = [8m, 9m, 10m, 11m, 11m, 12m];

        var units = new List<SiteUnit>();
        for (int i = 0; i < 6; i++)
            units.Add(new SiteUnit { Number = $"A-{i + 1}", Block = "A", Floor = i + 1, SquareMeters = 80 + (i % 3) * 10, BlockId = blokA.Id, ArsaPay = arsaPayiA[i] });
        for (int i = 0; i < 6; i++)
            units.Add(new SiteUnit { Number = $"B-{i + 1}", Block = "B", Floor = i + 1, SquareMeters = 85 + (i % 3) * 10, BlockId = blokB.Id, ArsaPay = arsaPayiB[i] });
        sdb.Units.AddRange(units);
        await sdb.SaveChangesAsync();

        await AddGeneratedResidents(sdb, units, 10, "lale", new Random(11));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 800, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.ArsaPayi };
        sdb.FeeSchedules.Add(schedule);
        await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, units, 2026, 3, 2026, 5);
        await AddAnnouncements(sdb, "Lale Apartmanı Yönetimi");
        await AddMaintenance(sdb, units, new Random(11));
        await AddMeetings(sdb, "Lale Apartmanı Yönetimi", "Lale Apartmanı", 800);
        await AddAccounting(sdb, 800 * 12, 2026, 3, 2026, 5, "Lale Apartmanı Yönetimi", new Random(11));
    }

    private static async Task SeedGunesSitesi(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "gunes-sitesi";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var gunesCompanyId = (await firmDb.Companies.FirstAsync()).Id;
        var gunesSiteProfile = new SiteProfile
        {
            CompanyId = gunesCompanyId,
            Name = "Güneş Sitesi", Slug = slug, SiteType = SiteType.Site,
            Address = "Güneş Bulvarı No:12, Çankaya", City = "Ankara", UnitCount = 24, DbFilePath = dbPath
        };
        firmDb.Sites.Add(gunesSiteProfile);
        await firmDb.SaveChangesAsync();
        firmDb.SiteContracts.Add(new SiteContract
        {
            SiteId = gunesSiteProfile.Id, StartDate = new DateOnly(2022, 6, 1), EndDate = new DateOnly(2025, 5, 31),
            MonthlyFee = 5800, Notes = "3 yıllık sözleşme. Güvenlik ve temizlik hizmetleri dahildir.",
            Status = ContractStatus.Active, Scope = ContractScope.Tumu, FeeType = ManagementFeeType.Fixed
        });
        await firmDb.SaveChangesAsync();

        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;

        var blokA = new SiteBlock { Name = "A Blok", Code = "A", FloorCount = 5, UnitCount = 10 };
        var blokB = new SiteBlock { Name = "B Blok", Code = "B", FloorCount = 5, UnitCount = 10 };
        sdb.Blocks.AddRange(blokA, blokB);
        await sdb.SaveChangesAsync();

        var units = new List<SiteUnit>();
        for (int i = 1; i <= 10; i++)
            units.Add(new SiteUnit { Number = $"A-{i}", Block = "A", Floor = ((i - 1) / 2) + 1, SquareMeters = 90 + (i % 3) * 15, BlockId = blokA.Id, UnitType = UnitType.Daire, ArsaPay = 18 + (i % 5) });
        for (int i = 1; i <= 10; i++)
            units.Add(new SiteUnit { Number = $"B-{i}", Block = "B", Floor = ((i - 1) / 2) + 1, SquareMeters = 95 + (i % 4) * 10, BlockId = blokB.Id, UnitType = UnitType.Daire, ArsaPay = 20 + (i % 4) });
        for (int i = 1; i <= 4; i++)
            units.Add(new SiteUnit { Number = $"D-{i}", Block = null, Floor = 0, SquareMeters = 60 + i * 10, UnitType = UnitType.Dukkan, ArsaPay = 30 + i * 3 });
        sdb.Units.AddRange(units);
        await sdb.SaveChangesAsync();

        await AddGeneratedResidents(sdb, units.Where(u => u.UnitType == UnitType.Daire).ToList(), 17, "gunes", new Random(22));
        var daireSch  = new SiteFeeSchedule { Name = "Konut Aidatı",  Amount = 1200, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.EsitPay, AppliesToUnitType = UnitType.Daire };
        var dukkanSch = new SiteFeeSchedule { Name = "Dükkan Aidatı", Amount = 2500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.EsitPay, AppliesToUnitType = UnitType.Dukkan };
        sdb.FeeSchedules.AddRange(daireSch, dukkanSch);
        await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, daireSch,  units.Where(u => u.UnitType == UnitType.Daire).ToList(),  2026, 3, 2026, 5);
        await GeneratePayments(sdb, dukkanSch, units.Where(u => u.UnitType == UnitType.Dukkan).ToList(), 2026, 3, 2026, 5);
        await AddAnnouncements(sdb, "Güneş Sitesi Yönetimi");
        await AddMaintenance(sdb, units, new Random(22));
        await AddMeetings(sdb, "Güneş Sitesi Yönetimi", "Güneş Sitesi", 1200);
        await AddAccounting(sdb, 1200 * 20 + 2500 * 4, 2026, 3, 2026, 5, "Güneş Sitesi Yönetimi", new Random(22));
    }

    // ─── SEVGİ YÖNETİM ───────────────────────────────────────────────────────

    private static async Task SeedBaharSitesi(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "bahar-sitesi";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var baharCompanyId = (await firmDb.Companies.FirstAsync()).Id;
        var baharSiteProfile = new SiteProfile
        {
            CompanyId = baharCompanyId,
            Name = "Bahar Sitesi", Slug = slug, SiteType = SiteType.Site,
            Address = "Bahar Sokak No:8, Kadıköy", City = "İstanbul", UnitCount = 36, DbFilePath = dbPath
        };
        firmDb.Sites.Add(baharSiteProfile);
        await firmDb.SaveChangesAsync();
        firmDb.SiteContracts.Add(new SiteContract
        {
            SiteId = baharSiteProfile.Id, StartDate = new DateOnly(2023, 1, 1), EndDate = new DateOnly(2025, 12, 31),
            MonthlyFee = 8500, Notes = "2 yıllık yönetim sözleşmesi. Yıllık TÜFE+5 artış hakkı mevcuttur.",
            Status = ContractStatus.Active, Scope = ContractScope.Tumu, FeeType = ManagementFeeType.Fixed
        });
        await firmDb.SaveChangesAsync();

        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;

        var blokA = new SiteBlock { Name = "A Blok", Code = "A", FloorCount = 4, UnitCount = 12 };
        var blokB = new SiteBlock { Name = "B Blok", Code = "B", FloorCount = 4, UnitCount = 12 };
        var blokC = new SiteBlock { Name = "C Blok", Code = "C", FloorCount = 4, UnitCount = 12 };
        sdb.Blocks.AddRange(blokA, blokB, blokC);
        await sdb.SaveChangesAsync();

        var blockMap = new[] { (blokA, "A"), (blokB, "B"), (blokC, "C") };
        var units = new List<SiteUnit>();
        foreach (var (blok, code) in blockMap)
            for (int i = 1; i <= 12; i++)
                units.Add(new SiteUnit
                {
                    Number = $"{code}-{i}", Block = code, Floor = ((i - 1) / 3) + 1,
                    SquareMeters = 85 + (i % 4) * 15, BlockId = blok.Id,
                    UnitType = UnitType.Daire, ArsaPay = 10 + (i % 8) + (code == "A" ? 0 : code == "B" ? 2 : 4)
                });
        sdb.Units.AddRange(units);
        await sdb.SaveChangesAsync();

        await AddGeneratedResidents(sdb, units, 29, "bahar", new Random(101));
        var schedule = new SiteFeeSchedule { Name = "Aylık Aidat", Amount = 1500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.MetreKare };
        sdb.FeeSchedules.Add(schedule);
        await sdb.SaveChangesAsync();
        await GeneratePayments(sdb, schedule, units, 2026, 3, 2026, 5);
        await AddAnnouncements(sdb, "Bahar Sitesi Yönetimi");
        await AddMaintenance(sdb, units, new Random(101));
        await AddMeetings(sdb, "Bahar Sitesi Yönetimi", "Bahar Sitesi", 1500);
        await AddAccounting(sdb, 1500 * 36, 2026, 3, 2026, 5, "Bahar Sitesi Yönetimi", new Random(101));
    }

    private static async Task SeedPrestijTopluYapi(FirmDbContext firmDb, SiteDbContextFactory factory)
    {
        const string slug = "prestij-toplu-yapi";
        var dbPath = Path.Combine("data", "sites", $"{slug}.db");
        var prestijCompanyId = (await firmDb.Companies.FirstAsync()).Id;
        var prestijSiteProfile = new SiteProfile
        {
            CompanyId = prestijCompanyId,
            Name = "Prestij Toplu Yapı", Slug = slug, SiteType = SiteType.TopluYapi,
            Address = "Prestij Caddesi No:200, Ataşehir", City = "İstanbul", UnitCount = 80, DbFilePath = dbPath
        };
        firmDb.Sites.Add(prestijSiteProfile);
        await firmDb.SaveChangesAsync();
        firmDb.SiteContracts.Add(new SiteContract
        {
            SiteId = prestijSiteProfile.Id, StartDate = new DateOnly(2022, 6, 1), EndDate = new DateOnly(2027, 5, 31),
            MonthlyFee = 35000, Notes = "5 yıllık premium yönetim sözleşmesi. 7/24 güvenlik ve teknik ekip dahildir.",
            Status = ContractStatus.Active, Scope = ContractScope.Tumu, FeeType = ManagementFeeType.Fixed
        });
        await firmDb.SaveChangesAsync();

        await using var sdb = await factory.CreateAndMigrateAsync(dbPath);
        if (await sdb.Units.AnyAsync()) return;

        // 2 kısım × 2 blok
        var k1a = new SiteBlock { Name = "1.Kısım A Blok", Code = "1A", FloorCount = 8, UnitCount = 16 };
        var k1b = new SiteBlock { Name = "1.Kısım B Blok", Code = "1B", FloorCount = 8, UnitCount = 16 };
        var k2a = new SiteBlock { Name = "2.Kısım A Blok", Code = "2A", FloorCount = 6, UnitCount = 12 };
        var k2b = new SiteBlock { Name = "2.Kısım B Blok", Code = "2B", FloorCount = 6, UnitCount = 12 };
        sdb.Blocks.AddRange(k1a, k1b, k2a, k2b);
        await sdb.SaveChangesAsync();

        var units = new List<SiteUnit>();

        // 1.Kısım — sadece daireler
        for (int i = 1; i <= 16; i++)
            units.Add(new SiteUnit { Number = $"1A-{i}", Block = "1A", Floor = ((i - 1) / 2) + 1, SquareMeters = 100 + (i % 5) * 20, BlockId = k1a.Id, UnitType = UnitType.Daire, ArsaPay = 12 + (i % 6) });
        for (int i = 1; i <= 16; i++)
            units.Add(new SiteUnit { Number = $"1B-{i}", Block = "1B", Floor = ((i - 1) / 2) + 1, SquareMeters = 110 + (i % 4) * 15, BlockId = k1b.Id, UnitType = UnitType.Daire, ArsaPay = 14 + (i % 5) });

        // 2.Kısım — daire + dükkan + otopark
        for (int i = 1; i <= 12; i++)
            units.Add(new SiteUnit { Number = $"2A-{i}", Block = "2A", Floor = ((i - 1) / 2) + 1, SquareMeters = 95 + (i % 4) * 15, BlockId = k2a.Id, UnitType = UnitType.Daire, ArsaPay = 10 + (i % 7) });
        for (int i = 1; i <= 4; i++)
            units.Add(new SiteUnit { Number = $"2A-D{i}", Block = "2A", Floor = 0, SquareMeters = 50 + i * 20, BlockId = k2a.Id, UnitType = UnitType.Dukkan, ArsaPay = 25 + i * 5 });
        for (int i = 1; i <= 12; i++)
            units.Add(new SiteUnit { Number = $"2B-{i}", Block = "2B", Floor = ((i - 1) / 2) + 1, SquareMeters = 90 + (i % 5) * 18, BlockId = k2b.Id, UnitType = UnitType.Daire, ArsaPay = 11 + (i % 6) });
        for (int i = 1; i <= 8; i++)
            units.Add(new SiteUnit { Number = $"OT-{i}", Block = "2B", Floor = -1, SquareMeters = 12, BlockId = k2b.Id, UnitType = UnitType.Otopark, ArsaPay = 3 });

        sdb.Units.AddRange(units);
        await sdb.SaveChangesAsync();

        var daireUnits   = units.Where(u => u.UnitType == UnitType.Daire).ToList();
        var dukkanUnits  = units.Where(u => u.UnitType == UnitType.Dukkan).ToList();
        var otoparkUnits = units.Where(u => u.UnitType == UnitType.Otopark).ToList();

        await AddGeneratedResidents(sdb, daireUnits, 48, "prestij", new Random(303));

        var daireSch   = new SiteFeeSchedule { Name = "Konut Aidatı",  Amount = 3000, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.ArsaPayi,  AppliesToUnitType = UnitType.Daire };
        var dukkanSch  = new SiteFeeSchedule { Name = "Dükkan Aidatı", Amount = 5000, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.EsitPay,   AppliesToUnitType = UnitType.Dukkan };
        var otoparkSch = new SiteFeeSchedule { Name = "Otopark Aidatı", Amount = 500, Period = FeePeriod.Monthly, DueDay = 10, StartDate = new DateOnly(2024, 1, 1), DistributionType = DistributionType.EsitPay,   AppliesToUnitType = UnitType.Otopark };
        sdb.FeeSchedules.AddRange(daireSch, dukkanSch, otoparkSch);
        await sdb.SaveChangesAsync();

        await GeneratePayments(sdb, daireSch,   daireUnits,   2026, 3, 2026, 5);
        await GeneratePayments(sdb, dukkanSch,  dukkanUnits,  2026, 3, 2026, 5);
        await GeneratePayments(sdb, otoparkSch, otoparkUnits, 2026, 3, 2026, 5);
        await AddAnnouncements(sdb, "Prestij Toplu Yapı Yönetimi");
        await AddMaintenance(sdb, units, new Random(303));
        await AddMeetings(sdb, "Prestij Toplu Yapı Yönetimi", "Prestij Toplu Yapı", 3000);
        await AddAccounting(sdb, 3000 * 56 + 5000 * 4 + 500 * 8, 2026, 3, 2026, 5, "Prestij Toplu Yapı Yönetimi", new Random(303));
    }

    // ─── PAKET & ABONELİK HELPERS ────────────────────────────────────────────

    private static async Task SeedMissingSubscriptions(MainDbContext db)
    {
        var kucukPkg = await db.FirmPackages.FirstOrDefaultAsync(p => p.Name == "Küçük");
        if (kucukPkg is null) return;

        if (!await db.FirmSubscriptions.AnyAsync(s => s.FirmSlug == "ozgur-yonetim"))
        {
            db.FirmSubscriptions.Add(new FirmSubscription
            {
                FirmSlug = "ozgur-yonetim", FirmPackageId = kucukPkg.Id,
                ContractStartDate = new DateOnly(2025, 1, 1), ContractEndDate = new DateOnly(2025, 12, 31),
                Status = SubscriptionStatus.Active, LastModifiedBy = "admin@ay.com", LastModifiedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await SeedPaymentHistory(db, "ozgur-yonetim", kucukPkg.MonthlyPrice, 2025, 1, 2026, 6);
        }

        if (!await db.FirmSubscriptions.AnyAsync(s => s.FirmSlug == "sevgi-yonetim"))
        {
            db.FirmSubscriptions.Add(new FirmSubscription
            {
                FirmSlug = "sevgi-yonetim", FirmPackageId = kucukPkg.Id,
                ContractStartDate = new DateOnly(2024, 6, 1), ContractEndDate = new DateOnly(2025, 5, 31),
                Status = SubscriptionStatus.Overdue,
                Notes = "Sözleşme yenileme görüşmeleri devam ediyor.",
                LastModifiedBy = "admin@ay.com", LastModifiedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            await SeedPaymentHistory(db, "sevgi-yonetim", kucukPkg.MonthlyPrice, 2024, 6, 2026, 6, overdueFromMonth: (2026, 4));
        }
    }

    private static async Task SeedPackages(MainDbContext db)
    {
        if (await db.FirmPackages.AnyAsync()) return;
        var packages = new[]
        {
            new FirmPackage { Name = "Solo",      MinSiteCount = 1,  MaxSiteCount = 1,  MonthlyPrice =   399m, DisplayOrder = 1 },
            new FirmPackage { Name = "Küçük",     MinSiteCount = 2,  MaxSiteCount = 5,  MonthlyPrice =   999m, DisplayOrder = 2 },
            new FirmPackage { Name = "Orta",      MinSiteCount = 6,  MaxSiteCount = 15, MonthlyPrice =  2499m, DisplayOrder = 3 },
            new FirmPackage { Name = "Büyük",     MinSiteCount = 16, MaxSiteCount = 30, MonthlyPrice =  4999m, DisplayOrder = 4 },
            new FirmPackage { Name = "Kurumsal",  MinSiteCount = 31, MaxSiteCount = 50, MonthlyPrice =  8999m, DisplayOrder = 5 },
            new FirmPackage { Name = "Enterprise",MinSiteCount = 51, MaxSiteCount = null, MonthlyPrice = 0m,   DisplayOrder = 6 },
        };
        db.FirmPackages.AddRange(packages);
        await db.SaveChangesAsync();
    }


    private static async Task SeedBillingConfig(MainDbContext db)
    {
        if (await db.SiteBillingConfigs.AnyAsync()) return;
        db.SiteBillingConfigs.Add(new SiteBillingConfig
        {
            Id = 1, PricePerDaire = 15m, PricePerBlok = 50m, PricePerKisim = 100m,
            MinimumMonthly = 100m, DefaultPeriod = BillingPeriod.Monthly,
            UpdatedBy = "system"
        });
        await db.SaveChangesAsync();
    }

    private static async Task MigrateExistingFirmDbsAsync(MainDbContext db, FirmDbContextFactory firmFactory)
    {
        var firms = await db.FirmRegistrations.ToListAsync();
        foreach (var firm in firms)
        {
            try
            {
                await using var firmDb = firmFactory.CreateBySlug(firm.Slug);
                await firmDb.Database.MigrateAsync();
            }
            catch { /* firma DB yoksa veya erişilemiyorsa atla */ }
        }
    }

    private static async Task SeedMissingStaffDataAsync(MainDbContext db, UserManager<AppUser> userManager, FirmDbContextFactory firmFactory)
    {
        var firms = await db.FirmRegistrations.ToListAsync();
        foreach (var firm in firms)
        {
            try
            {
                await using var firmDb = firmFactory.CreateBySlug(firm.Slug);
                var company = await firmDb.Companies.FirstOrDefaultAsync();
                if (company is null) continue;

                // CompanyStaff kaydı olmayanlar için oluştur
                var firmUsers = await userManager.Users.Where(u => u.FirmSlug == firm.Slug).ToListAsync();
                foreach (var user in firmUsers)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    // Manager (SiteAdmin) için CompanyStaff oluşturma — onlar firma sahibi
                    if (roles.Contains("Manager")) continue;

                    if (await firmDb.CompanyStaff.AnyAsync(s => s.UserId == user.Id)) continue;

                    StaffRole role = roles.Contains("Auditor") ? StaffRole.Auditor
                        : roles.Contains("Accountant") ? StaffRole.Accountant
                        : StaffRole.SiteManager;

                    firmDb.CompanyStaff.Add(new CompanyStaff
                    {
                        CompanyId = company.Id, UserId = user.Id,
                        Role = role, IsActive = true
                    });
                }
                await firmDb.SaveChangesAsync();
            }
            catch { /* firma DB yoksa atla */ }
        }
    }

    private static async Task SeedMissingObligations(MainDbContext db, FirmDbContextFactory firmFactory)
    {
        var cfg = await db.SiteBillingConfigs.FirstOrDefaultAsync() ?? new SiteBillingConfig();
        var firms = await db.FirmRegistrations.ToListAsync();

        foreach (var firm in firms)
        {
            try
            {
                await using var firmDb = firmFactory.Create(firm.DbFilePath);
                var sites = await firmDb.Sites.Where(s => s.IsActive).ToListAsync();
                foreach (var site in sites)
                {
                    if (await db.SiteObligations.AnyAsync(o => o.SiteId == site.Id)) continue;

                    int blokCount = 0, kisimCount = 0;
                    decimal monthly = Math.Max(site.UnitCount * cfg.PricePerDaire, cfg.MinimumMonthly);

                    if (site.SiteType != SiteType.Apartman && System.IO.File.Exists(site.DbFilePath))
                    {
                        try
                        {
                            var siteOpts = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SiteDbContext>()
                                .UseSqlite($"DataSource={site.DbFilePath};Cache=Shared")
                                .Options;
                            await using var siteDb = new SiteDbContext(siteOpts);
                            blokCount = await siteDb.Blocks.CountAsync();
                            kisimCount = await siteDb.Kisimlar.CountAsync();
                        }
                        catch { }
                        monthly = site.SiteType switch
                        {
                            SiteType.Site => Math.Max(site.UnitCount * cfg.PricePerDaire + blokCount * cfg.PricePerBlok, cfg.MinimumMonthly),
                            SiteType.TopluYapi => Math.Max(site.UnitCount * cfg.PricePerDaire + blokCount * cfg.PricePerBlok + kisimCount * cfg.PricePerKisim, cfg.MinimumMonthly),
                            _ => monthly
                        };
                    }

                    db.SiteObligations.Add(new SiteObligation
                    {
                        FirmSlug = firm.Slug,
                        SiteId = site.Id,
                        SiteName = site.Name,
                        SiteType = site.SiteType,
                        DaireCount = site.UnitCount,
                        BlokCount = blokCount,
                        KisimCount = kisimCount,
                        MonthlyAmount = monthly,
                        BillingPeriod = cfg.DefaultPeriod,
                        PricePerDaire = cfg.PricePerDaire,
                        PricePerBlok = cfg.PricePerBlok,
                        PricePerKisim = cfg.PricePerKisim,
                    });
                }
                await db.SaveChangesAsync();
            }
            catch { /* FirmDB erişilemiyor ise atla */ }
        }
    }

    private static async Task SeedPaymentHistory(MainDbContext db, string firmSlug, decimal amount,
        int fromYear, int fromMonth, int toYear, int toMonth,
        (int year, int month)? overdueFromMonth = null)
    {
        var rnd = new Random(firmSlug.GetHashCode());
        var (cy, cm) = (fromYear, fromMonth);
        while (cy < toYear || (cy == toYear && cm <= toMonth))
        {
            var dueDate = new DateOnly(cy, cm, 5);
            var isOverdue = overdueFromMonth.HasValue &&
                (cy > overdueFromMonth.Value.year || (cy == overdueFromMonth.Value.year && cm >= overdueFromMonth.Value.month));
            var isFuture = new DateOnly(cy, cm, 1) > new DateOnly(2026, 6, 1);
            PaymentRecordStatus status;
            DateOnly? paymentDate = null;
            decimal paid;
            if (isFuture) { status = PaymentRecordStatus.Pending; paid = 0; }
            else if (isOverdue) { status = PaymentRecordStatus.Overdue; paid = 0; }
            else
            {
                var r = rnd.NextDouble();
                if (r > 0.1) { status = PaymentRecordStatus.Paid; paid = amount; paymentDate = dueDate.AddDays(rnd.Next(0, 8)); }
                else { status = PaymentRecordStatus.Partial; paid = amount / 2; }
            }
            db.FirmPaymentRecords.Add(new FirmPaymentRecord
            {
                FirmSlug = firmSlug, PeriodYear = cy, PeriodMonth = cm,
                AmountDue = amount, AmountPaid = paid,
                DueDate = dueDate, PaymentDate = paymentDate, PaymentStatus = status
            });
            if (cm == 12) { cy++; cm = 1; } else cm++;
        }
        await db.SaveChangesAsync();
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static async Task<AppUser> EnsureUser(UserManager<AppUser> um, string email, string password,
        string displayName, string? firmSlug, string[] roles)
    {
        var user = await um.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName, FirmSlug = firmSlug };
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
            var email = $"{sitePrefix}.{unit.Number.ToLower().Replace("-", "").Replace(".", "")}@ornek.com";
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
        List<SiteUnit> units, int fromYear, int fromMonth, int toYear, int toMonth)
    {
        var rnd = new Random(42);
        var (cy, cm) = (fromYear, fromMonth);
        while (cy < toYear || (cy == toYear && cm <= toMonth))
        {
            var label = PeriodLabel(cy, cm);
            var dueDate = new DateOnly(cy, cm, Math.Min(schedule.DueDay, DateTime.DaysInMonth(cy, cm)));
            foreach (var unit in units)
            {
                var isPast = new DateOnly(cy, cm, 1) < new DateOnly(2026, 6, 1);
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

    private static async Task AddAnnouncements(SiteDbContext db, string author)
    {
        var now = DateTime.Now;
        var items = new[]
        {
            ("Asansör Periyodik Bakımı", "Binamızın asansörü periyodik yıllık bakıma alınacaktır. Bakım süresince lütfen merdivenleri kullanınız.", false, now.AddDays(-200)),
            ("Su Kesintisi Duyurusu", "Yarın 08:00-17:00 saatleri arasında ana hat bakımı nedeniyle su kesintisi yaşanacaktır.", true, now.AddDays(-155)),
            ("Ortak Alan Temizlik Takvimi", "Ortak alanlar her Salı ve Cuma 09:00-13:00 arasında temizlenmektedir.", false, now.AddDays(-130)),
            ("2024 Olağan Genel Kurul", "2024 yılı genel kurul toplantımız yapılacaktır. Tüm kat maliklerinin katılımı önemlidir.", false, now.AddDays(-100)),
            ("Güvenlik Sistemi Güncellendi", "Ortak alanlara yeni nesil IP kameralar kurulmuş, 7/24 kayıt sistemi devreye girmiştir.", false, now.AddDays(-80)),
            ("Bahçe ve Peyzaj Çalışması", "1-7 Mayıs tarihleri arasında bahçe peyzaj yenilemesi yapılacaktır.", false, now.AddDays(-65)),
            ("Elektrik Paneli Bakımı", "Ana elektrik panosunun bakımı gerçekleştirilecek.", true, now.AddDays(-50)),
            ("Dış Cephe Boyama Çalışması", "Haziran ayı boyunca bina dış cephesi yenilenecektir.", false, now.AddDays(-35)),
            ("Otopark Numaralandırma Sistemi", "Otopark yerleri yeniden numaralandırılmıştır. Araç plaka bilgilerinizi iletiniz.", false, now.AddDays(-22)),
            ("Doğalgaz Sayaç Değişimi", "15-20 Haziran tarihleri arasında sayaç değişimi yapılacaktır.", true, now.AddDays(-5)),
            ("Çocuk Parkı Yenilendi", "Site içindeki çocuk oyun alanı komple yenilendi. Ailelere hayırlı olsun!", false, now.AddDays(-2)),
        };
        foreach (var (title, content, urgent, date) in items)
            db.Announcements.Add(new SiteAnnouncement
            {
                Title = title, Content = content, IsUrgent = urgent,
                PublishedAt = date, CreatedBy = author, ExpiresAt = urgent ? date.AddDays(30) : null
            });
        await db.SaveChangesAsync();
    }

    private static async Task AddMaintenance(SiteDbContext db, List<SiteUnit> units, Random rnd)
    {
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
            ("Yangın tüpü süresi dolmuş", "Yangın tüplerinin son kullanma tarihi geçmiş.", "Güvenlik", MaintenancePriority.High, MaintenanceStatus.Resolved, "Tüm blokların yangın tüpleri yenilendi."),
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
             "1. Açılış ve yoklama\n2. Divan seçimi\n3. 2023 faaliyet raporu\n4. Bütçe onayı\n5. Aidat belirlenmesi",
             $"2023 yılı hesapları incelendi, yönetim ibra edildi. 2024 aylık aidat {aidat:N0} TL olarak belirlendi.",
             $"Aidat {aidat:N0} TL, yönetim ibra edildi.", 20 + (int)(aidat / 200)),

            ("Acil Toplantı – Asansör Yenileme", MeetingType.Emergency, MeetingStatus.Completed,
             new DateTime(2024, 5, 10, 19, 0, 0), "Toplantı Salonu",
             "1. Asansör arızaları ve yenileme teklifleri\n2. Finansman seçenekleri",
             "Üç firmadan teklif değerlendirildi. En uygun firma seçildi.",
             "Asansör yenileme anlaşması yapıldı.", 15 + (int)(aidat / 300)),

            ("2024 Yıl Sonu ve 2025 Bütçe", MeetingType.Ordinary, MeetingStatus.Completed,
             new DateTime(2024, 12, 14, 15, 0, 0), "Toplantı Salonu",
             "1. 2024 yıl değerlendirmesi\n2. 2025 tahmini bütçe\n3. Aidat revizyonu",
             $"Enflasyon göz önünde bulundurularak aidat artışı görüşüldü.",
             $"2025 aidatı %15 artışla {newAidat:N0} TL oldu.", 22 + (int)(aidat / 250)),

            ($"{siteName} 2025 Olağan Genel Kurulu", MeetingType.Ordinary, MeetingStatus.Completed,
             new DateTime(2025, 3, 22, 14, 0, 0), "Toplantı Salonu",
             "1. Açılış ve divan seçimi\n2. 2024 faaliyet raporu\n3. 2025 bütçe onayı",
             "2024 yılı hesapları incelendi. Yönetim kurulu ibra edildi.",
             "Yönetim kurulu yenilendi, 2025 bütçesi onaylandı.", 25 + (int)(aidat / 200)),

            ("2025 Kış Hazırlıkları", MeetingType.Ordinary, MeetingStatus.Scheduled,
             now.AddDays(12).Date.AddHours(19), "Toplantı Salonu",
             "1. Isıtma sistemi yıllık bakımı\n2. Kış bütçesi", null, null, 0),

            ($"{siteName} 2026 Genel Kurul Hazırlığı", MeetingType.Ordinary, MeetingStatus.Scheduled,
             now.AddDays(35).Date.AddHours(14), "Toplantı Salonu",
             "1. 2025 yılı faaliyet raporu\n2. 2026 bütçe taslağı\n3. Aidat revizyonu", null, null, 0),
        };

        foreach (var (title, type, status, date, location, agenda, minutesContent, decisions, attendees) in meetings)
        {
            var meeting = new SiteMeeting { Title = title, MeetingType = type, Status = status, MeetingDate = date, Location = location, AgendaItems = agenda, CreatedBy = author };
            db.Meetings.Add(meeting);
            await db.SaveChangesAsync();
            if (status == MeetingStatus.Completed && minutesContent is not null)
            {
                db.MeetingMinutes.Add(new SiteMeetingMinutes { MeetingId = meeting.Id, Content = minutesContent, AttendeeCount = attendees, Decisions = decisions });
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task AddAccounting(SiteDbContext db, decimal totalAidat,
        int fromYear, int fromMonth, int toYear, int toMonth, string author, Random rnd)
    {
        var (cy, cm) = (fromYear, fromMonth);
        while (cy < toYear || (cy == toYear && cm <= toMonth))
        {
            var date = new DateOnly(cy, cm, 1);
            db.AccountingEntries.Add(new SiteAccountingEntry
            {
                Type = AccountingEntryType.Income, Category = "Aidat Geliri",
                Amount = totalAidat * (decimal)(0.78 + rnd.NextDouble() * 0.15),
                Date = date.AddDays(15), Description = $"{PeriodLabel(cy, cm)} aidat tahsilatı", CreatedBy = author
            });
            decimal elec     = 800  + totalAidat / 100 + rnd.Next(-100, 300);
            decimal water    = 400  + totalAidat / 200 + rnd.Next(-50,  150);
            decimal clean    = 600  + totalAidat / 80  + rnd.Next(-50,  100);
            decimal security = 2000 + totalAidat / 50  + rnd.Next(-100, 300);
            decimal mgmt     = totalAidat / 60;
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Elektrik",         Amount = elec,     Date = date.AddDays(5),  Description = $"{PeriodLabel(cy, cm)} elektrik faturası",   CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Su",              Amount = water,    Date = date.AddDays(8),  Description = $"{PeriodLabel(cy, cm)} su faturası",          CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Temizlik",        Amount = clean,    Date = date.AddDays(1),  Description = $"{PeriodLabel(cy, cm)} temizlik hizmeti",     CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Güvenlik",        Amount = security, Date = date.AddDays(1),  Description = $"{PeriodLabel(cy, cm)} güvenlik hizmeti",     CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Yönetim Ücreti", Amount = mgmt,     Date = date.AddDays(1),  Description = $"{PeriodLabel(cy, cm)} yönetim ücreti",      CreatedBy = author });
            db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Asansör Bakımı", Amount = 300 + rnd.Next(0, 200), Date = date.AddDays(10), Description = $"{PeriodLabel(cy, cm)} asansör bakım sözleşmesi", CreatedBy = author });
            if (cm is >= 10 or <= 4)
                db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = "Doğalgaz", Amount = 1200 + totalAidat / 80 + rnd.Next(-200, 600), Date = date.AddDays(12), Description = $"{PeriodLabel(cy, cm)} doğalgaz faturası", CreatedBy = author });
            if (rnd.NextDouble() > 0.55)
            {
                var cats = new[] { "Bakım-Onarım", "Sigorta", "Peyzaj", "Kırtasiye", "Hukuki Gider", "Banka Komisyonu" };
                var cat = cats[rnd.Next(cats.Length)];
                db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Expense, Category = cat, Amount = 200 + rnd.Next(100, (int)(totalAidat / 20)), Date = date.AddDays(rnd.Next(2, 28)), Description = $"{PeriodLabel(cy, cm)} {cat.ToLowerInvariant()} gideri", CreatedBy = author });
            }
            if (rnd.NextDouble() > 0.7)
                db.AccountingEntries.Add(new SiteAccountingEntry { Type = AccountingEntryType.Income, Category = "Diğer Gelir", Amount = 500 + rnd.Next(200, 2000), Date = date.AddDays(rnd.Next(5, 25)), Description = $"{PeriodLabel(cy, cm)} otopark / reklam panosu geliri", CreatedBy = author });
            if (cm == 12) { cy++; cm = 1; } else cm++;
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedEgitimAsync(MainDbContext db, UserManager<AppUser> userManager)
    {
        if (await db.Egitimler.AnyAsync()) return;

        await EnsureUser(userManager, "egitim@ay.com", "Egitim1234!", "Eğitim Koordinatörü", null, ["EgitimAdmin"]);

        var egitim = new Domain.Entities.Egitim.Egitim
        {
            Ad = "Profesyonel Site Yöneticiliği Sertifika Programı",
            Aciklama = "Kat Mülkiyeti Kanunu, aidat yönetimi, muhasebe, sakin iletişimi ve dijital yönetim araçlarını kapsayan kapsamlı sertifika programı. Programı başarıyla tamamlayan katılımcılara ApartNet onaylı sertifika verilmektedir.",
            Hedefler = "• KMK hükümlerini eksiksiz uygulama\n• Aidat ve muhasebe yönetimini profesyonelce yürütme\n• Sakin ve mülk sahibi iletişimini etkin yönetme\n• Dijital apartman yönetim sistemlerini kullanma\n• Yasal uyuşmazlıklarda doğru adımları atma",
            Gereksinimler = "Lise mezunu olmak, temel bilgisayar kullanımı, apartman veya site yönetimi alanında çalışmak veya çalışmayı planlamak.",
            SertifikaAdi = "Profesyonel Site Yöneticisi Sertifikası",
            IsActive = true
        };
        db.Egitimler.Add(egitim);
        await db.SaveChangesAsync();

        var baslangic = new DateOnly(2026, 7, 7);
        var donem = new Domain.Entities.Egitim.EgitimDonemi
        {
            EgitimId = egitim.Id,
            Ad = "2026 Yaz Dönemi — Temmuz Kohort",
            BaslangicTarihi = baslangic,
            BitisTarihi = new DateOnly(2026, 8, 1),
            Kontenjan = 20,
            Fiyat = 4500,
            Tur = Domain.Enums.EgitimTuru.Karma,
            Konum = "ApartNet Eğitim Merkezi, Çanakkale",
            OnlinePlatform = "Zoom / ApartNet Portal",
            Durum = Domain.Enums.DonemDurumu.Planlandi
        };
        db.EgitimDonemleri.Add(donem);
        await db.SaveChangesAsync();

        var dersler = new[]
        {
            (1,  "Site Yönetimine Giriş ve Mesleki Etik",               "Apartman ve site yönetiminin tanımı, yöneticinin hakları ve sorumlulukları, mesleki etik ilkeleri."),
            (2,  "Kat Mülkiyeti Kanunu Temelleri",                       "634 sayılı KMK'nın temel hükümleri, kat irtifakı ve kat mülkiyeti farkı, önemli maddeler."),
            (3,  "Yönetim Planı Hazırlama ve Uygulama",                  "Yönetim planının yasal dayanağı, içeriği, nasıl hazırlanacağı ve uygulanacağı."),
            (4,  "Kat Malikleri Kurulu ve Toplantı Yönetimi",            "Olağan/olağanüstü toplantı çağrısı, nisap hesaplama, karar alma süreçleri, tutanak yazımı."),
            (5,  "Aidat Hesaplama Yöntemleri",                           "Gider paylaşım esasları, arsa payı hesabı, aidat belirleme formülleri, özel gider kalemleri."),
            (6,  "Aidat Tahsilatı ve İcra Takibi",                       "Borçlu kat malikine yasal yollar, icra süreci, gecikme zammı hesabı, pratik tahsilat yöntemleri."),
            (7,  "Temel Muhasebe ve Gelir-Gider Kaydı",                  "Çift taraflı kayıt sistemi, gelir ve gider kalemleri, makbuz düzenleme, muhasebe programları."),
            (8,  "Bütçe Planlaması ve Mali Raporlama",                   "Yıllık bütçe hazırlama, gerçekleşme takibi, kat maliklerine mali rapor sunumu."),
            (9,  "Bakım-Onarım Yönetimi",                                "Periyodik bakım planı, acil onarım süreçleri, ihale ve sözleşme yapılması, iş takibi."),
            (10, "Arıza Takip Sistemleri ve Kayıt Tutma",                "Arıza bildirim süreçleri, önceliklendirme, müteahhit yönetimi, dijital takip sistemleri."),
            (11, "Sakin İletişimi ve Şikayet Yönetimi",                  "Etkili iletişim teknikleri, şikayet ele alma prosedürü, zor sakinlerle başa çıkma yöntemleri."),
            (12, "Sigorta İşlemleri ve Risk Yönetimi",                   "Zorunlu deprem sigortası (DASK), bina sigortası türleri, hasar bildirimi, risk azaltma."),
            (13, "Sözleşme Hazırlama ve Hizmet Alımı",                   "Temizlik, güvenlik, asansör, bahçe hizmet sözleşmeleri, sözleşmede bulunması gerekenler."),
            (14, "İş Sağlığı, Güvenliği ve Mevzuat",                     "Bina çalışanlarının ISG yükümlülükleri, asansör güvenliği, yangın önlemleri, yasal gereklilikler."),
            (15, "Vergi Yükümlülükleri ve Beyannameler",                 "Apartman yönetiminin vergi mükellefiyeti, muhtasar beyanname, SGK bildirimleri."),
            (16, "Dijital Apartman Yönetim Sistemleri",                  "ApartNet ve benzer yazılımların kullanımı, dijital aidat takibi, online sakin iletişimi."),
            (17, "Raporlama Teknikleri ve Arşivleme",                    "Aylık/yıllık rapor formatları, belge arşivleme sistemi, dijital depolama, kayıt saklama süreleri."),
            (18, "Hukuki Uyuşmazlıklar ve Çözüm Yolları",               "Kat malikleri arası anlaşmazlıklar, sulh hukuk mahkemesi süreci, arabuluculuk, pratik örnekler."),
            (19, "Personel Yönetimi ve İş Hukuku",                       "Kapıcı/görevli istihdamı, iş sözleşmesi, kıdem/ihbar tazminatı, disiplin prosedürleri."),
            (20, "Vaka Analizleri, Sınav ve Sertifika Töreni",           "Gerçek vaka incelemeleri, yazılı sınav (70 puan geçer not), başarı belgesi ve sertifika takdimi.")
        };

        for (int i = 0; i < dersler.Length; i++)
        {
            var (sira, baslik, aciklama) = dersler[i];
            int haftaOffset = i / 5;
            int gunOffset = i % 5;
            var dersTarihi = baslangic.ToDateTime(TimeOnly.Parse("09:30")).AddDays(haftaOffset * 7 + gunOffset);
            db.DersProgramlari.Add(new Domain.Entities.Egitim.DersProgrami
            {
                DonemId = donem.Id,
                SiraNo = sira,
                Baslik = baslik,
                Aciklama = aciklama,
                DersTarihi = dersTarihi,
                SureDakika = sira == 20 ? 180 : 90
            });
        }
        await db.SaveChangesAsync();

        var kursiyerVerisi = new[]
        {
            ("Ahmet",    "Yılmaz",    "ahmet.yilmaz@email.com",  "05321234567", "İstanbul",   "Apartman Yöneticisi",  4500m, Domain.Enums.OdemeDurumu.Tamamlandi),
            ("Fatma",    "Kaya",      "fatma.kaya@email.com",    "05334567890", "Ankara",     "Muhasebeci",           4500m, Domain.Enums.OdemeDurumu.Tamamlandi),
            ("Mehmet",   "Demir",     "mehmet.demir@email.com",  "05359876543", "İzmir",      "Site Görevlisi",       2250m, Domain.Enums.OdemeDurumu.KismiOdeme),
            ("Ayşe",     "Çelik",     "ayse.celik@email.com",    "05443216789", "Bursa",      "Emlak Danışmanı",      4500m, Domain.Enums.OdemeDurumu.Tamamlandi),
            ("Mustafa",  "Arslan",    "mustafa.arslan@email.com","05467891234", "Antalya",    "Apartman Yöneticisi",  4500m, Domain.Enums.OdemeDurumu.Tamamlandi),
            ("Zeynep",   "Koç",       "zeynep.koc@email.com",    "05512345678", "Çanakkale",  "Öğrenci",              0m,    Domain.Enums.OdemeDurumu.Bekliyor),
            ("Hasan",    "Şahin",     "hasan.sahin@email.com",   "05378901234", "Balıkesir",  "Site Yöneticisi",      4500m, Domain.Enums.OdemeDurumu.Tamamlandi),
            ("Elif",     "Yıldız",    "elif.yildiz@email.com",   "05423456789", "Edirne",     "Muhasebeci",           4500m, Domain.Enums.OdemeDurumu.Tamamlandi),
        };

        var dersListesi = await db.DersProgramlari.Where(d => d.DonemId == donem.Id).OrderBy(d => d.SiraNo).ToListAsync();

        foreach (var (ad, soyad, email, tel, sehir, meslek, odenen, odemeDurumu) in kursiyerVerisi)
        {
            var kursiyer = new Domain.Entities.Egitim.Kursiyer
            {
                DonemId = donem.Id,
                Ad = ad, Soyad = soyad, Email = email, Telefon = tel,
                Sehir = sehir, Meslek = meslek,
                OdemeDurumu = odemeDurumu,
                OdenenTutar = odenen,
                KayitTarihi = new DateOnly(2026, 6, 15)
            };
            db.Kursiyerler.Add(kursiyer);
            await db.SaveChangesAsync();

            var rnd = new Random(kursiyer.Id);
            foreach (var ders in dersListesi.Take(5))
            {
                db.DersTakipleri.Add(new Domain.Entities.Egitim.DersTakibi
                {
                    KursiyerId = kursiyer.Id,
                    DersProgramiId = ders.Id,
                    Katildi = odemeDurumu != Domain.Enums.OdemeDurumu.Bekliyor && rnd.NextDouble() > 0.15
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
