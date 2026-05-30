using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace ApartmanYonetim.Infrastructure.Identity;

public class AppUserClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<AppUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("DisplayName", user.DisplayName ?? user.Email ?? string.Empty));
        if (user.FirmSlug is not null)
            identity.AddClaim(new Claim("firm_slug", user.FirmSlug));
        if (user.SiteId is not null)
            identity.AddClaim(new Claim("site_id", user.SiteId.Value.ToString()));
        return identity;
    }
}
