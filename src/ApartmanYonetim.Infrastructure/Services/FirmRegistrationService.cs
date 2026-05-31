using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class FirmRegistrationService(MainDbContext db, FirmDbContextFactory firmFactory, SiteDbContextFactory siteFactory) : IFirmRegistrationService
{
    public async Task<List<FirmRegDto>> GetAllAsync()
        => await db.FirmRegistrations.OrderBy(f => f.Name)
            .Select(f => new FirmRegDto(f.Id, f.Name, f.Slug, f.IsActive, f.CreatedAt))
            .ToListAsync();

    public async Task<FirmRegDto> CreateAsync(FirmRegCommand cmd)
    {
        var dbPath = firmFactory.ResolvePath(cmd.Slug);
        var reg = new FirmRegistration { Name = cmd.Name, Slug = cmd.Slug, DbFilePath = dbPath };
        db.FirmRegistrations.Add(reg);
        await db.SaveChangesAsync();

        await using var firmDb = await firmFactory.CreateAndMigrateAsync(cmd.Slug);
        if (!await firmDb.Companies.AnyAsync())
        {
            firmDb.Companies.Add(new ManagementCompany { Name = cmd.Name, Slug = cmd.Slug });
            await firmDb.SaveChangesAsync();
        }

        return new FirmRegDto(reg.Id, reg.Name, reg.Slug, reg.IsActive, reg.CreatedAt);
    }

    public async Task UpdateAsync(Guid id, FirmRegCommand cmd)
    {
        var reg = await db.FirmRegistrations.FindAsync(id) ?? throw new InvalidOperationException("Firma bulunamadı.");
        reg.Name = cmd.Name;
        await db.SaveChangesAsync();

        await using var firmDb = firmFactory.CreateBySlug(reg.Slug);
        var company = await firmDb.Companies.FirstOrDefaultAsync();
        if (company is not null) { company.Name = cmd.Name; await firmDb.SaveChangesAsync(); }
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
        => await db.FirmRegistrations.AnyAsync(f => f.Slug == slug && (excludeId == null || f.Id != excludeId));

    public async Task<List<AdminSiteDto>> GetAllSitesAsync()
    {
        var firms = await db.FirmRegistrations.ToListAsync();
        var result = new List<AdminSiteDto>();
        foreach (var firm in firms)
        {
            try
            {
                await using var firmDb = firmFactory.CreateBySlug(firm.Slug);
                var sites = await firmDb.Sites.Include(s => s.Company).ToListAsync();
                foreach (var s in sites)
                    result.Add(new AdminSiteDto(s.Id, s.CompanyId, firm.Slug, firm.Name, s.Name, s.Slug,
                        s.City, s.UnitCount, s.DbFilePath, s.IsActive,
                        s.ContractStartDate, s.ContractEndDate, s.MonthlyManagementFee, s.ContractNotes, s.SiteType));
            }
            catch { /* skip firm if db not accessible */ }
        }
        return result.OrderBy(s => s.Name).ToList();
    }

    public async Task<AdminSiteDto> CreateSiteForFirmAsync(string firmSlug, SiteCommand cmd)
    {
        await using var firmDb = await firmFactory.CreateAndMigrateAsync(firmSlug);
        var company = await firmDb.Companies.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Firma DB'sinde şirket kaydı bulunamadı.");
        var dbPath = siteFactory.ResolvePath(Path.Combine("data", "sites", $"{cmd.Slug}.db"));
        var site = new SiteProfile
        {
            CompanyId = company.Id, Name = cmd.Name, Slug = cmd.Slug,
            Address = cmd.Address, City = cmd.City, UnitCount = cmd.UnitCount,
            SiteType = cmd.SiteType, DbFilePath = dbPath,
            ContractStartDate = cmd.ContractStartDate, ContractEndDate = cmd.ContractEndDate,
            MonthlyManagementFee = cmd.MonthlyManagementFee, ContractNotes = cmd.ContractNotes
        };
        firmDb.Sites.Add(site);
        await firmDb.SaveChangesAsync();
        await using var siteDb = await siteFactory.CreateAndMigrateAsync(dbPath);
        if (cmd.SiteType == SiteType.Apartman && cmd.UnitCount > 0)
        {
            for (var i = 1; i <= cmd.UnitCount; i++)
                siteDb.Units.Add(new SiteUnit { Number = i.ToString() });
            await siteDb.SaveChangesAsync();
        }
        return new AdminSiteDto(site.Id, site.CompanyId, firmSlug, company.Name, site.Name, site.Slug,
            site.City, site.UnitCount, site.DbFilePath, site.IsActive,
            site.ContractStartDate, site.ContractEndDate, site.MonthlyManagementFee, site.ContractNotes, site.SiteType);
    }
}
