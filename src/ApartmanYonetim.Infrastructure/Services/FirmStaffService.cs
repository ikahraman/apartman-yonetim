using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Identity;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApartmanYonetim.Infrastructure.Services;

public class FirmStaffService(
    UserManager<AppUser> userManager,
    FirmDbContext firmDb) : IFirmStaffService
{
    public async Task<List<StaffDto>> GetByFirmAsync(string firmSlug)
    {
        var company = await firmDb.Companies.FirstOrDefaultAsync();
        if (company is null) return [];

        var staffList = await firmDb.CompanyStaff
            .Where(s => s.CompanyId == company.Id)
            .Include(s => s.SiteAssignments.Where(a => a.RemovedAt == null))
                .ThenInclude(a => a.Site)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        var result = new List<StaffDto>();
        foreach (var staff in staffList)
        {
            var user = await userManager.FindByIdAsync(staff.UserId);
            if (user is null) continue;
            var siteNames = staff.SiteAssignments
                .Where(a => a.RemovedAt == null)
                .Select(a => a.Site?.Name ?? "")
                .Where(n => n.Length > 0)
                .ToList();
            result.Add(new StaffDto(
                staff.Id.ToString(),
                staff.UserId,
                user.DisplayName ?? user.Email ?? "",
                user.Email,
                staff.Role,
                siteNames,
                staff.IsActive,
                staff.CreatedAt));
        }
        return result;
    }

    public async Task<StaffDto> CreateAsync(string firmSlug, CreateStaffCommand cmd, string createdByUserId)
    {
        var company = await firmDb.Companies.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Firma kaydı bulunamadı.");

        var user = new AppUser
        {
            UserName = cmd.Email, Email = cmd.Email,
            EmailConfirmed = true, DisplayName = cmd.DisplayName,
            FirmSlug = firmSlug
        };
        var result = await userManager.CreateAsync(user, cmd.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));

        var identityRole = cmd.Role switch
        {
            StaffRole.SiteAdmin    => "Manager",
            StaffRole.SiteManager  => "SiteManager",
            StaffRole.Accountant   => "Accountant",
            StaffRole.Auditor      => "Auditor",
            _                      => "SiteManager"
        };
        await userManager.AddToRoleAsync(user, identityRole);

        var staff = new CompanyStaff
        {
            CompanyId = company.Id,
            UserId = user.Id,
            Role = cmd.Role,
            IsActive = true,
            CreatedByUserId = createdByUserId
        };
        firmDb.CompanyStaff.Add(staff);
        await firmDb.SaveChangesAsync();

        return new StaffDto(staff.Id.ToString(), user.Id, user.DisplayName ?? "", user.Email, cmd.Role, [], true, staff.CreatedAt);
    }

    public async Task UpdateSiteAccessAsync(string staffId, List<Guid> siteIds, string assignedByUserId)
    {
        var id = Guid.Parse(staffId);
        var existing = await firmDb.SiteStaffAssignments
            .Where(a => a.StaffId == id && a.RemovedAt == null)
            .ToListAsync();
        foreach (var e in existing)
            e.RemovedAt = DateTime.UtcNow;

        foreach (var sid in siteIds)
            firmDb.SiteStaffAssignments.Add(new SiteStaffAssignment
            {
                StaffId = id, SiteId = sid, AssignedByUserId = assignedByUserId
            });
        await firmDb.SaveChangesAsync();
    }

    public async Task DeactivateAsync(string staffId)
    {
        var id = Guid.Parse(staffId);
        var staff = await firmDb.CompanyStaff.FindAsync(id) ?? throw new KeyNotFoundException();
        staff.IsActive = false;
        staff.DeactivatedAt = DateTime.UtcNow;
        await firmDb.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(staff.UserId);
        if (user is not null)
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
    }

    public async Task ReactivateAsync(string staffId)
    {
        var id = Guid.Parse(staffId);
        var staff = await firmDb.CompanyStaff.FindAsync(id) ?? throw new KeyNotFoundException();
        staff.IsActive = true;
        staff.DeactivatedAt = null;
        await firmDb.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(staff.UserId);
        if (user is not null)
            await userManager.SetLockoutEndDateAsync(user, null);
    }

    public async Task<List<ManagerLookupDto>> GetManagersByFirmAsync(string firmSlug)
    {
        var users = await userManager.Users
            .Where(u => u.FirmSlug == firmSlug)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();
        var result = new List<ManagerLookupDto>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            if (roles.Contains("Manager"))
                result.Add(new ManagerLookupDto(u.Id, u.DisplayName ?? u.Email ?? "", u.Email));
        }
        return result;
    }
}
