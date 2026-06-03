using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteManagementService(FirmDbContext db, SiteDbContextFactory factory, FirmDbContextFactory firmFactory, MainDbContext mainDb) : ISiteManagementService
{
    private static SiteDto ToDto(SiteProfile s)
    {
        var contract = s.Contracts.Where(c => c.Status == ContractStatus.Active).MaxBy(c => c.StartDate);
        return new SiteDto(s.Id, s.CompanyId, s.Company?.Name ?? "", s.Name, s.Slug,
            s.Address, s.City, s.UnitCount, s.DbFilePath, s.IsActive,
            contract?.StartDate, contract?.EndDate, contract?.MonthlyFee, contract?.Notes, s.SiteType);
    }

    private IQueryable<SiteProfile> SitesWithData()
        => db.Sites.Include(s => s.Company).Include(s => s.Contracts);

    public async Task<List<SiteDto>> GetAllAsync()
        => await SitesWithData().OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();

    public async Task<List<SiteDto>> GetByCompanyAsync(Guid companyId)
        => await SitesWithData().Where(s => s.CompanyId == companyId).OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();

    public async Task<List<SiteDto>> GetForUserAsync(string userId)
    {
        var staffIds = await db.CompanyStaff
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync();

        if (staffIds.Count == 0)
            return await SitesWithData().OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();

        var siteIds = await db.SiteStaffAssignments
            .Where(a => staffIds.Contains(a.StaffId) && a.RemovedAt == null)
            .Select(a => a.SiteId)
            .Distinct()
            .ToListAsync();

        if (siteIds.Count == 0)
            return await SitesWithData().OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();

        return await SitesWithData()
            .Where(s => siteIds.Contains(s.Id))
            .OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();
    }

    public async Task<SiteDto?> GetByIdAsync(Guid id)
    {
        var s = await SitesWithData().FirstOrDefaultAsync(s => s.Id == id);
        if (s is not null) return ToDto(s);

        var slugs = await mainDb.FirmRegistrations.Select(f => f.Slug).ToListAsync();
        foreach (var slug in slugs)
        {
            try
            {
                await using var firmDb = firmFactory.CreateBySlug(slug);
                var site = await firmDb.Sites.Include(x => x.Company).Include(x => x.Contracts).FirstOrDefaultAsync(x => x.Id == id);
                if (site is not null) return ToDto(site);
            }
            catch { }
        }
        return null;
    }

    public async Task<SiteDto> CreateAsync(SiteCommand cmd)
    {
        var company = await db.Companies.FindAsync(cmd.CompanyId) ?? throw new InvalidOperationException("Firma bulunamadı.");
        var dbPath = Path.Combine("data", "sites", $"{cmd.Slug}.db");
        var site = new SiteProfile
        {
            CompanyId = cmd.CompanyId, Name = cmd.Name, Slug = cmd.Slug,
            Address = cmd.Address, City = cmd.City, UnitCount = cmd.UnitCount,
            SiteType = cmd.SiteType, DbFilePath = dbPath
        };
        db.Sites.Add(site);
        await db.SaveChangesAsync();

        if (cmd.ContractStartDate.HasValue)
        {
            db.SiteContracts.Add(new SiteContract
            {
                SiteId = site.Id,
                StartDate = cmd.ContractStartDate.Value,
                EndDate = cmd.ContractEndDate,
                MonthlyFee = cmd.MonthlyManagementFee ?? 0,
                Notes = cmd.ContractNotes,
                Status = ContractStatus.Active,
                Scope = ContractScope.Tumu,
                FeeType = ManagementFeeType.Fixed
            });
            await db.SaveChangesAsync();
        }

        await factory.CreateAndMigrateAsync(dbPath);
        var result = await SitesWithData().FirstAsync(s => s.Id == site.Id);
        return ToDto(result);
    }

    public async Task UpdateAsync(Guid id, SiteCommand cmd)
    {
        var s = await db.Sites.FindAsync(id) ?? throw new InvalidOperationException("Site bulunamadı.");
        s.Name = cmd.Name; s.Slug = cmd.Slug; s.SiteType = cmd.SiteType;
        s.Address = cmd.Address; s.City = cmd.City; s.UnitCount = cmd.UnitCount;
        await db.SaveChangesAsync();

        // Update active contract if contract info provided
        if (cmd.ContractStartDate.HasValue)
        {
            var active = await db.SiteContracts
                .Where(c => c.SiteId == id && c.Status == ContractStatus.Active)
                .OrderByDescending(c => c.StartDate)
                .FirstOrDefaultAsync();
            if (active is not null)
            {
                active.StartDate = cmd.ContractStartDate.Value;
                active.EndDate = cmd.ContractEndDate;
                active.MonthlyFee = cmd.MonthlyManagementFee ?? active.MonthlyFee;
                active.Notes = cmd.ContractNotes;
                await db.SaveChangesAsync();
            }
            else
            {
                db.SiteContracts.Add(new SiteContract
                {
                    SiteId = id,
                    StartDate = cmd.ContractStartDate.Value,
                    EndDate = cmd.ContractEndDate,
                    MonthlyFee = cmd.MonthlyManagementFee ?? 0,
                    Notes = cmd.ContractNotes,
                    Status = ContractStatus.Active,
                    Scope = ContractScope.Tumu,
                    FeeType = ManagementFeeType.Fixed
                });
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task<List<Guid>> GetUserSiteIdsAsync(string userId)
    {
        var staffIds = await db.CompanyStaff
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync();
        if (staffIds.Count == 0) return [];
        return await db.SiteStaffAssignments
            .Where(a => staffIds.Contains(a.StaffId) && a.RemovedAt == null)
            .Select(a => a.SiteId)
            .Distinct()
            .ToListAsync();
    }

    public async Task SetUserSiteAccessAsync(string userId, List<Guid> siteIds)
    {
        var staff = await db.CompanyStaff.FirstOrDefaultAsync(s => s.UserId == userId);
        if (staff is null) return;
        var existing = await db.SiteStaffAssignments
            .Where(a => a.StaffId == staff.Id && a.RemovedAt == null)
            .ToListAsync();
        foreach (var e in existing) e.RemovedAt = DateTime.UtcNow;
        foreach (var siteId in siteIds)
            db.SiteStaffAssignments.Add(new SiteStaffAssignment { StaffId = staff.Id, SiteId = siteId });
        await db.SaveChangesAsync();
    }

    public async Task<List<SiteDto>> GetAllByFirmSlugAsync(string firmSlug)
    {
        await using var firmDb = firmFactory.CreateBySlug(firmSlug);
        return await firmDb.Sites.Include(s => s.Company).Include(s => s.Contracts).OrderBy(s => s.Name).Select(s => ToDto(s)).ToListAsync();
    }
}
