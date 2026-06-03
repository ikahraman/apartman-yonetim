using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class ManagementCompanyService(FirmDbContext db) : IManagementCompanyService
{
    private static CompanyDto ToDto(ManagementCompany c, int siteCount) =>
        new(c.Id, c.Name, c.Slug, c.Email, c.Phone, c.Address,
            c.ContactPerson, c.Website, c.LogoUrl, c.IsActive, siteCount);

    public async Task<List<CompanyDto>> GetAllAsync()
        => await db.Companies.Include(c => c.Sites)
            .OrderBy(c => c.Name)
            .Select(c => ToDto(c, c.Sites.Count))
            .ToListAsync();

    public async Task<List<CompanyDto>> GetForUserAsync(string userId)
        => await GetAllAsync();

    public async Task<CompanyDto> CreateAsync(CompanyCommand cmd)
    {
        var company = new ManagementCompany
        {
            Name = cmd.Name, Slug = cmd.Slug,
            Email = cmd.Email, Phone = cmd.Phone, Address = cmd.Address,
            ContactPerson = cmd.ContactPerson, Website = cmd.Website, LogoUrl = cmd.LogoUrl
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return ToDto(company, 0);
    }

    public async Task UpdateAsync(Guid id, CompanyCommand cmd)
    {
        var c = await db.Companies.FindAsync(id) ?? throw new InvalidOperationException("Firma bulunamadı.");
        c.Name = cmd.Name; c.Slug = cmd.Slug;
        c.Email = cmd.Email; c.Phone = cmd.Phone; c.Address = cmd.Address;
        c.ContactPerson = cmd.ContactPerson; c.Website = cmd.Website; c.LogoUrl = cmd.LogoUrl;
        await db.SaveChangesAsync();
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
        => await db.Companies.AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId));
}
