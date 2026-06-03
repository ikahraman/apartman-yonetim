using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record StaffDto(
    string Id,           // CompanyStaff.Id.ToString()
    string UserId,       // AppUser.Id
    string DisplayName,
    string? Email,
    StaffRole Role,
    List<string> SiteNames,
    bool IsActive,
    DateTime CreatedAt);

public record CreateStaffCommand(string DisplayName, string Email, string Password, StaffRole Role);
public record UpdateStaffRoleCommand(StaffRole Role);
public record ManagerLookupDto(string Id, string DisplayName, string? Email);

public interface IFirmStaffService
{
    Task<List<StaffDto>> GetByFirmAsync(string firmSlug);
    Task<List<ManagerLookupDto>> GetManagersByFirmAsync(string firmSlug);
    Task<StaffDto> CreateAsync(string firmSlug, CreateStaffCommand cmd, string createdByUserId);
    Task UpdateSiteAccessAsync(string staffId, List<Guid> siteIds, string assignedByUserId);
    Task DeactivateAsync(string staffId);
    Task ReactivateAsync(string staffId);
}
