using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApartmanYonetim.Infrastructure.Services;

public class FirmSubscriptionService(MainDbContext db, FirmDbContextFactory firmFactory) : IFirmSubscriptionService
{
    // ── Paketler ─────────────────────────────────────────────────────────────

    public async Task<List<PackageDto>> GetPackagesAsync() =>
        await db.FirmPackages
            .OrderBy(p => p.DisplayOrder)
            .Select(p => ToPackageDto(p))
            .ToListAsync();

    public async Task<PackageDto?> GetPackageByIdAsync(int id) =>
        await db.FirmPackages
            .Where(p => p.Id == id)
            .Select(p => ToPackageDto(p))
            .FirstOrDefaultAsync();

    public async Task<PackageDto> CreatePackageAsync(PackageCommand cmd)
    {
        var pkg = new FirmPackage
        {
            Name = cmd.Name, MinSiteCount = cmd.MinSiteCount, MaxSiteCount = cmd.MaxSiteCount,
            MonthlyPrice = cmd.MonthlyPrice, DisplayOrder = cmd.DisplayOrder
        };
        db.FirmPackages.Add(pkg);
        await db.SaveChangesAsync();
        return ToPackageDto(pkg);
    }

    public async Task UpdatePackageAsync(int id, PackageCommand cmd)
    {
        var pkg = await db.FirmPackages.FindAsync(id) ?? throw new KeyNotFoundException();
        pkg.Name = cmd.Name; pkg.MinSiteCount = cmd.MinSiteCount; pkg.MaxSiteCount = cmd.MaxSiteCount;
        pkg.MonthlyPrice = cmd.MonthlyPrice; pkg.DisplayOrder = cmd.DisplayOrder;
        await db.SaveChangesAsync();
    }

    public async Task SetPackageActiveAsync(int id, bool isActive)
    {
        var pkg = await db.FirmPackages.FindAsync(id) ?? throw new KeyNotFoundException();
        pkg.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    // ── Abonelikler ──────────────────────────────────────────────────────────

    public async Task<List<SubscriptionDto>> GetAllSubscriptionsAsync()
    {
        var subs = await db.FirmSubscriptions
            .Include(s => s.Package)
            .ToListAsync();
        var firms = await db.FirmRegistrations.ToListAsync();
        var result = new List<SubscriptionDto>();
        foreach (var s in subs)
        {
            var firm = firms.FirstOrDefault(f => f.Slug == s.FirmSlug);
            result.Add(await ToSubscriptionDto(s, firm?.Name ?? s.FirmSlug));
        }
        return result;
    }

    public async Task<SubscriptionDto?> GetSubscriptionByFirmAsync(string firmSlug)
    {
        var s = await db.FirmSubscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.FirmSlug == firmSlug);
        if (s is null) return null;
        var firm = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == firmSlug);
        return await ToSubscriptionDto(s, firm?.Name ?? firmSlug);
    }

    public async Task<SubscriptionDto> CreateSubscriptionAsync(SubscriptionCommand cmd, string modifiedBy)
    {
        var sub = new FirmSubscription
        {
            FirmSlug = cmd.FirmSlug, FirmPackageId = cmd.FirmPackageId,
            ContractStartDate = cmd.ContractStartDate, ContractEndDate = cmd.ContractEndDate,
            CustomMonthlyPrice = cmd.CustomMonthlyPrice, Status = cmd.Status,
            Notes = cmd.Notes, LastModifiedBy = modifiedBy, LastModifiedAt = DateTime.UtcNow
        };
        db.FirmSubscriptions.Add(sub);
        await db.SaveChangesAsync();
        await db.Entry(sub).Reference(s => s.Package).LoadAsync();
        var firm = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == cmd.FirmSlug);
        return await ToSubscriptionDto(sub, firm?.Name ?? cmd.FirmSlug);
    }

    public async Task UpdateSubscriptionAsync(int id, SubscriptionCommand cmd, string modifiedBy)
    {
        var sub = await db.FirmSubscriptions.FindAsync(id) ?? throw new KeyNotFoundException();
        sub.FirmPackageId = cmd.FirmPackageId; sub.ContractStartDate = cmd.ContractStartDate;
        sub.ContractEndDate = cmd.ContractEndDate; sub.CustomMonthlyPrice = cmd.CustomMonthlyPrice;
        sub.Status = cmd.Status; sub.Notes = cmd.Notes;
        sub.LastModifiedBy = modifiedBy; sub.LastModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    // ── Ödeme Kayıtları ──────────────────────────────────────────────────────

    public async Task<List<PaymentRecordDto>> GetPaymentRecordsAsync(string firmSlug) =>
        await db.FirmPaymentRecords
            .Where(p => p.FirmSlug == firmSlug)
            .OrderByDescending(p => p.PeriodYear).ThenByDescending(p => p.PeriodMonth)
            .Select(p => ToPaymentDto(p))
            .ToListAsync();

    public async Task<PaymentRecordDto> UpsertPaymentRecordAsync(PaymentRecordCommand cmd)
    {
        var existing = await db.FirmPaymentRecords
            .FirstOrDefaultAsync(p => p.FirmSlug == cmd.FirmSlug && p.PeriodYear == cmd.PeriodYear && p.PeriodMonth == cmd.PeriodMonth);
        if (existing is null)
        {
            existing = new FirmPaymentRecord { FirmSlug = cmd.FirmSlug };
            db.FirmPaymentRecords.Add(existing);
        }
        existing.PeriodYear = cmd.PeriodYear; existing.PeriodMonth = cmd.PeriodMonth;
        existing.AmountDue = cmd.AmountDue; existing.AmountPaid = cmd.AmountPaid;
        existing.DueDate = cmd.DueDate; existing.PaymentDate = cmd.PaymentDate;
        existing.PaymentStatus = cmd.PaymentStatus; existing.Notes = cmd.Notes;
        await db.SaveChangesAsync();
        return ToPaymentDto(existing);
    }

    public async Task DeletePaymentRecordAsync(int id)
    {
        var rec = await db.FirmPaymentRecords.FindAsync(id) ?? throw new KeyNotFoundException();
        db.FirmPaymentRecords.Remove(rec);
        await db.SaveChangesAsync();
    }

    // ── Limit Kontrolü ───────────────────────────────────────────────────────

    public async Task<(bool allowed, int currentCount, int? maxCount)> CheckSiteLimitAsync(string firmSlug)
    {
        var sub = await db.FirmSubscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.FirmSlug == firmSlug);
        if (sub is null) return (true, 0, null); // abonelik yoksa serbest

        var firmReg = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == firmSlug);
        if (firmReg is null) return (true, 0, null);

        int activeSiteCount;
        try
        {
            await using var firmDb = firmFactory.Create(firmReg.DbFilePath);
            activeSiteCount = await firmDb.Sites.CountAsync(s => s.IsActive);
        }
        catch { activeSiteCount = 0; }

        var maxCount = sub.Package?.MaxSiteCount;
        var allowed = maxCount is null || activeSiteCount < maxCount;
        return (allowed, activeSiteCount, maxCount);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PackageDto ToPackageDto(FirmPackage p) =>
        new(p.Id, p.Name, p.MinSiteCount, p.MaxSiteCount, p.MonthlyPrice, p.IsActive, p.DisplayOrder);

    private async Task<SubscriptionDto> ToSubscriptionDto(FirmSubscription s, string firmName)
    {
        var firmReg = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == s.FirmSlug);
        int activeSiteCount = 0;
        if (firmReg is not null)
        {
            try
            {
                await using var firmDb = firmFactory.Create(firmReg.DbFilePath);
                activeSiteCount = await firmDb.Sites.CountAsync(x => x.IsActive);
            }
            catch { }
        }
        var effective = s.CustomMonthlyPrice ?? s.Package?.MonthlyPrice ?? 0;
        return new SubscriptionDto(
            s.Id, s.FirmSlug, firmName,
            s.FirmPackageId, s.Package?.Name ?? "-", s.Package?.MaxSiteCount,
            s.ContractStartDate, s.ContractEndDate,
            s.CustomMonthlyPrice, effective,
            s.Status, s.Notes, s.LastModifiedBy, s.LastModifiedAt,
            activeSiteCount);
    }

    private static PaymentRecordDto ToPaymentDto(FirmPaymentRecord p) =>
        new(p.Id, p.FirmSlug, p.PeriodYear, p.PeriodMonth,
            p.AmountDue, p.AmountPaid, p.DueDate, p.PaymentDate,
            p.PaymentStatus, p.Notes);
}
