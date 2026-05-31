using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteAccountingService(SiteDbContextFactory factory) : ISiteAccountingService
{
    public async Task<List<AccountingEntryDto>> GetAllAsync(string dbFilePath, int? year = null, int? month = null)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var query = db.AccountingEntries.AsQueryable();
        if (year.HasValue) query = query.Where(e => e.Date.Year == year);
        if (month.HasValue) query = query.Where(e => e.Date.Month == month);
        var entries = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAt).ToListAsync();
        var unitIds = entries.Where(e => e.UnitId.HasValue).Select(e => e.UnitId!.Value).Distinct().ToList();
        var units = await db.Units.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Number);
        return entries.Select(e => new AccountingEntryDto(e.Id, e.Type, e.Category, e.Amount, e.Date, e.Description,
            e.UnitId, e.UnitId.HasValue && units.TryGetValue(e.UnitId.Value, out var n) ? n : null,
            e.CreatedBy, e.CreatedAt)).ToList();
    }

    public async Task<AccountingEntryDto> AddAsync(string dbFilePath, AccountingCommand cmd, string createdBy)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var e = new SiteAccountingEntry
        {
            Type = cmd.Type, Category = cmd.Category, Amount = cmd.Amount,
            Date = cmd.Date, Description = cmd.Description, UnitId = cmd.UnitId, CreatedBy = createdBy
        };
        db.AccountingEntries.Add(e);
        await db.SaveChangesAsync();
        return new AccountingEntryDto(e.Id, e.Type, e.Category, e.Amount, e.Date, e.Description, e.UnitId, null, e.CreatedBy, e.CreatedAt);
    }

    public async Task DeleteAsync(string dbFilePath, Guid id)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var e = await db.AccountingEntries.FindAsync(id) ?? throw new InvalidOperationException("Kayıt bulunamadı.");
        db.AccountingEntries.Remove(e);
        await db.SaveChangesAsync();
    }

    public async Task<AccountingSummaryDto> GetSummaryAsync(string dbFilePath, int? year = null, int? month = null)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var query = db.AccountingEntries.AsQueryable();
        if (year.HasValue) query = query.Where(e => e.Date.Year == year);
        if (month.HasValue) query = query.Where(e => e.Date.Month == month);
        var entries = await query.ToListAsync();
        var income = entries.Where(e => e.Type == AccountingEntryType.Income).Sum(e => e.Amount);
        var expense = entries.Where(e => e.Type == AccountingEntryType.Expense).Sum(e => e.Amount);
        return new AccountingSummaryDto(income, expense, income - expense);
    }
}
