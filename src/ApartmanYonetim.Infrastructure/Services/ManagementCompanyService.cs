using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class ManagementCompanyService(MainDbContext db) : IManagementCompanyService
{
    public async Task<List<CompanyDto>> GetAllAsync()
        => await db.Companies.Include(c => c.Sites)
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Slug, c.Email, c.Phone, c.Address, c.IsActive, c.Sites.Count))
            .ToListAsync();

    public async Task<List<CompanyDto>> GetForUserAsync(string userId)
        => await db.Companies.Include(c => c.Sites).Include(c => c.UserAccess)
            .Where(c => c.UserAccess.Any(a => a.UserId == userId))
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Name, c.Slug, c.Email, c.Phone, c.Address, c.IsActive, c.Sites.Count))
            .ToListAsync();

    public async Task<CompanyDto> CreateAsync(CompanyCommand cmd)
    {
        var company = new ManagementCompany
        {
            Name = cmd.Name, Slug = cmd.Slug,
            Email = cmd.Email, Phone = cmd.Phone, Address = cmd.Address
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return new CompanyDto(company.Id, company.Name, company.Slug, company.Email, company.Phone, company.Address, company.IsActive, 0);
    }

    public async Task UpdateAsync(Guid id, CompanyCommand cmd)
    {
        var c = await db.Companies.FindAsync(id) ?? throw new InvalidOperationException("Firma bulunamadı.");
        c.Name = cmd.Name; c.Slug = cmd.Slug;
        c.Email = cmd.Email; c.Phone = cmd.Phone; c.Address = cmd.Address;
        await db.SaveChangesAsync();
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
        => await db.Companies.AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId));
}
