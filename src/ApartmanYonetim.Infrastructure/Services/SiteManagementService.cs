using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteManagementService(FirmDbContext db, SiteDbContextFactory factory, FirmDbContextFactory firmFactory) : ISiteManagementService
{
    private static SiteDto ToDto(SiteProfile s) =>
        new(s.Id, s.CompanyId, s.Company?.Name ?? "", s.Name, s.Slug, s.Address, s.City, s.UnitCount, s.DbFilePath, s.IsActive,
            s.ContractStartDate, s.ContractEndDate, s.MonthlyManagementFee, s.ContractNotes);

    public async Task<List<SiteDto>> GetAllAsync()
        => await db.Sites.Include(s => s.Company).OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();

    public async Task<List<SiteDto>> GetByCompanyAsync(Guid companyId)
        => await db.Sites.Include(s => s.Company).Where(s => s.CompanyId == companyId).OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();

    public async Task<List<SiteDto>> GetForUserAsync(string userId)
    {
        var siteIds = await db.UserSiteAccess.Where(a => a.UserId == userId).Select(a => a.SiteId).ToListAsync();
        if (siteIds.Count == 0)
            return await db.Sites.Include(s => s.Company).OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();
        return await db.Sites.Include(s => s.Company)
            .Where(s => siteIds.Contains(s.Id))
            .OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();
    }

    public async Task<SiteDto?> GetByIdAsync(Guid id)
    {
        var s = await db.Sites.Include(s => s.Company).FirstOrDefaultAsync(s => s.Id == id);
        return s is null ? null : ToDto(s);
    }

    public async Task<SiteDto> CreateAsync(SiteCommand cmd)
    {
        var company = await db.Companies.FindAsync(cmd.CompanyId) ?? throw new InvalidOperationException("Firma bulunamadı.");
        var dbPath = Path.Combine("data", "sites", $"{cmd.Slug}.db");
        var site = new SiteProfile
        {
            CompanyId = cmd.CompanyId, Name = cmd.Name, Slug = cmd.Slug,
            Address = cmd.Address, City = cmd.City, UnitCount = cmd.UnitCount,
            DbFilePath = dbPath,
            ContractStartDate = cmd.ContractStartDate, ContractEndDate = cmd.ContractEndDate,
            MonthlyManagementFee = cmd.MonthlyManagementFee, ContractNotes = cmd.ContractNotes
        };
        db.Sites.Add(site);
        await db.SaveChangesAsync();
        await factory.CreateAndMigrateAsync(dbPath);
        site.Company = company;
        return ToDto(site);
    }

    public async Task UpdateAsync(Guid id, SiteCommand cmd)
    {
        var s = await db.Sites.FindAsync(id) ?? throw new InvalidOperationException("Site bulunamadı.");
        s.Name = cmd.Name; s.Slug = cmd.Slug;
        s.Address = cmd.Address; s.City = cmd.City; s.UnitCount = cmd.UnitCount;
        s.ContractStartDate = cmd.ContractStartDate; s.ContractEndDate = cmd.ContractEndDate;
        s.MonthlyManagementFee = cmd.MonthlyManagementFee; s.ContractNotes = cmd.ContractNotes;
        await db.SaveChangesAsync();
    }

    public async Task<List<Guid>> GetUserSiteIdsAsync(string userId)
        => await db.UserSiteAccess.Where(a => a.UserId == userId).Select(a => a.SiteId).ToListAsync();

    public async Task SetUserSiteAccessAsync(string userId, List<Guid> siteIds)
    {
        var existing = await db.UserSiteAccess.Where(a => a.UserId == userId).ToListAsync();
        db.UserSiteAccess.RemoveRange(existing);
        foreach (var siteId in siteIds)
            db.UserSiteAccess.Add(new UserSiteAccess { UserId = userId, SiteId = siteId });
        await db.SaveChangesAsync();
    }

    public async Task<List<SiteDto>> GetAllByFirmSlugAsync(string firmSlug)
    {
        await using var firmDb = firmFactory.CreateBySlug(firmSlug);
        return await firmDb.Sites.Include(s => s.Company).OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();
    }
}
