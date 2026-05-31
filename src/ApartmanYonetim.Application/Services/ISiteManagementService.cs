using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record SiteDto(Guid Id, Guid CompanyId, string CompanyName, string Name, string Slug, string? Address, string? City, int UnitCount, string DbFilePath, bool IsActive,
    DateOnly? ContractStartDate, DateOnly? ContractEndDate, decimal? MonthlyManagementFee, string? ContractNotes,
    SiteType SiteType = SiteType.Site);
public record SiteCommand(Guid CompanyId, string Name, string Slug, string? Address, string? City, int UnitCount,
    DateOnly? ContractStartDate, DateOnly? ContractEndDate, decimal? MonthlyManagementFee, string? ContractNotes,
    SiteType SiteType = SiteType.Site);

public interface ISiteManagementService
{
    Task<List<SiteDto>> GetAllAsync();
    Task<List<SiteDto>> GetByCompanyAsync(Guid companyId);
    Task<List<SiteDto>> GetForUserAsync(string userId);
    Task<SiteDto?> GetByIdAsync(Guid id);
    Task<SiteDto> CreateAsync(SiteCommand cmd);
    Task UpdateAsync(Guid id, SiteCommand cmd);
    Task<List<Guid>> GetUserSiteIdsAsync(string userId);
    Task SetUserSiteAccessAsync(string userId, List<Guid> siteIds);
    Task<List<SiteDto>> GetAllByFirmSlugAsync(string firmSlug);
}
