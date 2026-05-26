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
            // Works for HTTP requests and for Blazor Server during the initial handshake
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("firm_slug");
            return claim?.Value;
        }
    }

    public bool HasFirm => FirmSlug is not null;

    public void SetFirmSlug(string slug) => _overrideSlug = slug;
}
