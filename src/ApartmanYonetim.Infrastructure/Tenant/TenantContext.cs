using Microsoft.AspNetCore.Http;
namespace ApartmanYonetim.Infrastructure.Tenant;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _overrideSlug;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? FirmSlug
    {
        get
        {
            if (_overrideSlug is not null) return _overrideSlug;
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("firm_slug");
            return claim?.Value;
        }
    }

    public bool HasFirm => FirmSlug is not null;

    public string? UserRole
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null) return null;
            foreach (var role in new[] { "SuperAdmin", "FirmAdmin", "SiteManager", "Auditor", "Accountant", "Resident" })
                if (user.IsInRole(role)) return role;
            return null;
        }
    }

    public void SetFirmSlug(string slug) => _overrideSlug = slug;
}
