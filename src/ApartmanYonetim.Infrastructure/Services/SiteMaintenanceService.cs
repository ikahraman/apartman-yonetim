using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteMaintenanceService(SiteDbContextFactory factory) : ISiteMaintenanceService
{
    private async Task<MaintenanceRequestDto> ToDto(SiteMaintenanceRequest r, SiteDbContext db)
    {
        string? unitNumber = null;
        if (r.UnitId.HasValue)
            unitNumber = (await db.Units.FindAsync(r.UnitId.Value))?.Number;
        return new MaintenanceRequestDto(r.Id, r.UnitId, unitNumber, r.ReporterName, r.ReporterPhone,
            r.Title, r.Description, r.Category, r.Status, r.Priority,
            r.ReportedAt, r.AssignedTo, r.ResolvedAt, r.ResolutionNotes);
    }

    public async Task<List<MaintenanceRequestDto>> GetAllAsync(string dbFilePath, MaintenanceStatus? statusFilter = null)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var query = db.MaintenanceRequests.AsQueryable();
        if (statusFilter.HasValue) query = query.Where(r => r.Status == statusFilter);
        var items = await query.OrderByDescending(r => r.Priority).ThenByDescending(r => r.ReportedAt).ToListAsync();
        var unitIds = items.Where(r => r.UnitId.HasValue).Select(r => r.UnitId!.Value).Distinct().ToList();
        var units = await db.Units.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Number);
        return items.Select(r => new MaintenanceRequestDto(r.Id, r.UnitId,
            r.UnitId.HasValue && units.TryGetValue(r.UnitId.Value, out var n) ? n : null,
            r.ReporterName, r.ReporterPhone, r.Title, r.Description, r.Category,
            r.Status, r.Priority, r.ReportedAt, r.AssignedTo, r.ResolvedAt, r.ResolutionNotes)).ToList();
    }

    public async Task<MaintenanceRequestDto> AddAsync(string dbFilePath, MaintenanceFormCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = new SiteMaintenanceRequest
        {
            UnitId = cmd.UnitId, ReporterName = cmd.ReporterName, ReporterPhone = cmd.ReporterPhone,
            Title = cmd.Title, Description = cmd.Description, Category = cmd.Category, Priority = cmd.Priority
        };
        db.MaintenanceRequests.Add(r);
        await db.SaveChangesAsync();
        return await ToDto(r, db);
    }

    public async Task UpdateStatusAsync(string dbFilePath, Guid id, MaintenanceStatus status, string? assignedTo, string? notes)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = await db.MaintenanceRequests.FindAsync(id) ?? throw new InvalidOperationException("Talep bulunamadı.");
        r.Status = status; r.AssignedTo = assignedTo ?? r.AssignedTo;
        if (notes is not null) r.ResolutionNotes = notes;
        await db.SaveChangesAsync();
    }

    public async Task ResolveAsync(string dbFilePath, Guid id, string resolutionNotes)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = await db.MaintenanceRequests.FindAsync(id) ?? throw new InvalidOperationException("Talep bulunamadı.");
        r.Status = MaintenanceStatus.Resolved;
        r.ResolvedAt = DateTime.UtcNow;
        r.ResolutionNotes = resolutionNotes;
        await db.SaveChangesAsync();
    }

    public async Task<(int Open, int InProgress, int Resolved)> GetSummaryAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var counts = await db.MaintenanceRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        return (
            counts.FirstOrDefault(c => c.Status == MaintenanceStatus.Open)?.Count ?? 0,
            counts.FirstOrDefault(c => c.Status == MaintenanceStatus.InProgress)?.Count ?? 0,
            counts.FirstOrDefault(c => c.Status == MaintenanceStatus.Resolved)?.Count ?? 0);
    }
}
