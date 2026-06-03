using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record FirmRegDto(
    Guid Id, string Name, string Slug,
    string? TaxNumber, string? TaxOffice,
    bool IsActive, DateTime CreatedAt);

public record FirmRegCommand(string Name, string Slug, string? TaxNumber = null, string? TaxOffice = null);

public record AdminSiteDto(
    Guid Id, Guid CompanyId, string FirmSlug, string FirmName,
    string Name, string Slug, string? City, int UnitCount,
    string DbFilePath, bool IsActive,
    DateOnly? ContractStartDate, DateOnly? ContractEndDate,
    decimal? MonthlyManagementFee, string? ContractNotes,
    SiteType SiteType = SiteType.Site,
    Guid? ActiveContractId = null);

public interface IFirmRegistrationService
{
    Task<List<FirmRegDto>> GetAllAsync();
    Task<FirmRegDto> CreateAsync(FirmRegCommand cmd);
    Task UpdateAsync(Guid id, FirmRegCommand cmd);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
    Task<List<AdminSiteDto>> GetAllSitesAsync();
    Task<AdminSiteDto> CreateSiteForFirmAsync(string firmSlug, SiteCommand cmd);
}
