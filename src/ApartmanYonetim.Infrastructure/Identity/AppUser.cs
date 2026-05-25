using Microsoft.AspNetCore.Identity;
namespace ApartmanYonetim.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
