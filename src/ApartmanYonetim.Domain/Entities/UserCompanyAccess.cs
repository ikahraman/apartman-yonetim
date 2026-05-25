namespace ApartmanYonetim.Domain.Entities;

public class UserCompanyAccess
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string UserId { get; set; } = default!;
    public Guid CompanyId { get; set; }
    public ManagementCompany Company { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
