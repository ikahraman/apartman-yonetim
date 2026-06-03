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
            .Select(f => new FirmRegDto(f.Id, f.Name, f.Slug, f.TaxNumber, f.TaxOffice, f.IsActive, f.CreatedAt))
            .ToListAsync();

    public async Task<FirmRegDto> CreateAsync(FirmRegCommand cmd)
    {
        var dbPath = firmFactory.ResolvePath(cmd.Slug);
        var reg = new FirmRegistration
        {
            Name = cmd.Name, Slug = cmd.Slug, DbFilePath = dbPath,
            TaxNumber = cmd.TaxNumber, TaxOffice = cmd.TaxOffice
        };
        db.FirmRegistrations.Add(reg);
        await db.SaveChangesAsync();

        await using var firmDb = await firmFactory.CreateAndMigrateAsync(cmd.Slug);
        if (!await firmDb.Companies.AnyAsync())
        {
            firmDb.Companies.Add(new ManagementCompany { Name = cmd.Name, Slug = cmd.Slug });
            await firmDb.SaveChangesAsync();
        }

        return new FirmRegDto(reg.Id, reg.Name, reg.Slug, reg.TaxNumber, reg.TaxOffice, reg.IsActive, reg.CreatedAt);
    }

    public async Task UpdateAsync(Guid id, FirmRegCommand cmd)
    {
        var reg = await db.FirmRegistrations.FindAsync(id) ?? throw new InvalidOperationException("Firma bulunamadı.");
        reg.Name = cmd.Name;
        reg.TaxNumber = cmd.TaxNumber;
        reg.TaxOffice = cmd.TaxOffice;
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
                var sites = await firmDb.Sites
                    .Include(s => s.Company)
                    .Include(s => s.Contracts)
                    .ToListAsync();
                foreach (var s in sites)
                {
                    var contract = s.Contracts.Where(c => c.Status == ContractStatus.Active).MaxBy(c => c.StartDate);
                    result.Add(new AdminSiteDto(
                        s.Id, s.CompanyId, firm.Slug, firm.Name, s.Name, s.Slug,
                        s.City, s.UnitCount, s.DbFilePath, s.IsActive,
                        contract?.StartDate, contract?.EndDate,
                        contract?.MonthlyFee, contract?.Notes, s.SiteType,
                        contract?.Id));
                }
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
            SiteType = cmd.SiteType, DbFilePath = dbPath
        };
        firmDb.Sites.Add(site);
        await firmDb.SaveChangesAsync();

        Guid? contractId = null;
        if (cmd.ContractStartDate.HasValue)
        {
            var contract = new SiteContract
            {
                SiteId = site.Id,
                StartDate = cmd.ContractStartDate.Value,
                EndDate = cmd.ContractEndDate,
                MonthlyFee = cmd.MonthlyManagementFee ?? 0,
                Notes = cmd.ContractNotes,
                Status = ContractStatus.Active,
                Scope = ContractScope.Tumu,
                FeeType = ManagementFeeType.Fixed
            };
            firmDb.SiteContracts.Add(contract);
            await firmDb.SaveChangesAsync();
            contractId = contract.Id;
        }

        await using var siteDb = await siteFactory.CreateAndMigrateAsync(dbPath);
        if (cmd.SiteType == SiteType.Apartman && cmd.UnitCount > 0)
        {
            for (var i = 1; i <= cmd.UnitCount; i++)
                siteDb.Units.Add(new SiteUnit { Number = i.ToString() });
            await siteDb.SaveChangesAsync();
        }

        return new AdminSiteDto(site.Id, site.CompanyId, firmSlug, company.Name, site.Name, site.Slug,
            site.City, site.UnitCount, site.DbFilePath, site.IsActive,
            cmd.ContractStartDate, cmd.ContractEndDate, cmd.MonthlyManagementFee, cmd.ContractNotes,
            site.SiteType, contractId);
    }
}
