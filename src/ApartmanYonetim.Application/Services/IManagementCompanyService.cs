namespace ApartmanYonetim.Application.Services;

public record CompanyDto(Guid Id, string Name, string Slug, string? Email, string? Phone, string? Address, bool IsActive, int SiteCount);
public record CompanyCommand(string Name, string Slug, string? Email, string? Phone, string? Address);

public interface IManagementCompanyService
{
    Task<List<CompanyDto>> GetAllAsync();
    Task<List<CompanyDto>> GetForUserAsync(string userId);
    Task<CompanyDto> CreateAsync(CompanyCommand cmd);
    Task UpdateAsync(Guid id, CompanyCommand cmd);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null);
}
