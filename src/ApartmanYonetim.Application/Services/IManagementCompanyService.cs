namespace ApartmanYonetim.Application.Services;

public record CompanyDto(
    Guid Id, string Name, string Slug,
    string? Email, string? Phone, string? Address,
    string? ContactPerson, string? Website, string? LogoUrl,
    bool IsActive, int SiteCount);

public record CompanyCommand(
    string Name, string Slug,
    string? Email, string? Phone, string? Address,
    string? ContactPerson, string? Website, string? LogoUrl);

public interface IManagementCompanyService
{
    Task<List<CompanyDto>> GetAllAsync();
    Task<List<CompanyDto>> GetForUserAsync(string userId);
    Task<CompanyDto> CreateAsync(CompanyCommand cmd);
    Task UpdateAsync(Guid id, CompanyCommand cmd);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
}
