using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApartmanYonetim.Infrastructure.Services;

public class SiteObligationService(MainDbContext db) : ISiteObligationService
{
    // ── Tier Fiyatlandırma ────────────────────────────────────────────────────

    public async Task<List<SiteBillingTierDto>> GetTiersAsync() =>
        await db.SiteBillingTiers
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new SiteBillingTierDto(t.Id, t.MinDaire, t.MaxDaire, t.MonthlyAmount, t.DisplayOrder))
            .ToListAsync();

    public async Task<SiteBillingTierDto> UpsertTierAsync(int? id, SiteBillingTierCommand cmd)
    {
        SiteBillingTier tier;
        if (id.HasValue)
        {
            tier = await db.SiteBillingTiers.FindAsync(id.Value) ?? throw new KeyNotFoundException();
        }
        else
        {
            tier = new SiteBillingTier();
            db.SiteBillingTiers.Add(tier);
        }
        tier.MinDaire = cmd.MinDaire;
        tier.MaxDaire = cmd.MaxDaire;
        tier.MonthlyAmount = cmd.MonthlyAmount;
        tier.DisplayOrder = cmd.DisplayOrder;
        await db.SaveChangesAsync();
        return new SiteBillingTierDto(tier.Id, tier.MinDaire, tier.MaxDaire, tier.MonthlyAmount, tier.DisplayOrder);
    }

    public async Task DeleteTierAsync(int id)
    {
        var tier = await db.SiteBillingTiers.FindAsync(id) ?? throw new KeyNotFoundException();
        db.SiteBillingTiers.Remove(tier);
        await db.SaveChangesAsync();
    }

    public async Task<decimal> CalculateMonthlyAsync(int daireCount)
    {
        var tiers = await db.SiteBillingTiers.OrderBy(t => t.DisplayOrder).ToListAsync();
        return CalculateFromTiers(tiers, daireCount);
    }

    public async Task RecalculateAllObligationsAsync()
    {
        var tiers = await db.SiteBillingTiers.OrderBy(t => t.DisplayOrder).ToListAsync();
        var obs = await db.SiteObligations.Where(o => o.IsActive).ToListAsync();
        foreach (var ob in obs)
            ob.MonthlyAmount = CalculateFromTiers(tiers, ob.DaireCount);
        await db.SaveChangesAsync();
    }

    private static decimal CalculateFromTiers(List<SiteBillingTier> tiers, int daireCount)
    {
        if (daireCount <= 0) daireCount = 1;
        var tier = tiers.FirstOrDefault(t =>
            daireCount >= t.MinDaire && (t.MaxDaire == null || daireCount <= t.MaxDaire));
        // Eğer hiç tier yoksa ya da eşleşme yoksa son tier'ın fiyatı
        tier ??= tiers.OrderByDescending(t => t.MinDaire).FirstOrDefault();
        return tier?.MonthlyAmount ?? 0m;
    }

    // ── Yükümlülükler ─────────────────────────────────────────────────────────

    public async Task<List<SiteObligationDto>> GetAllObligationsAsync()
    {
        var obs = await db.SiteObligations.Include(o => o.Payments)
            .OrderBy(o => o.FirmSlug).ThenBy(o => o.SiteName).ToListAsync();
        var firmNames = await db.FirmRegistrations.ToDictionaryAsync(f => f.Slug, f => f.Name);
        return obs.Select(o => ToDto(o, firmNames.GetValueOrDefault(o.FirmSlug, o.FirmSlug))).ToList();
    }

    public async Task<List<SiteObligationDto>> GetObligationsByFirmAsync(string firmSlug)
    {
        var obs = await db.SiteObligations.Include(o => o.Payments)
            .Where(o => o.FirmSlug == firmSlug).OrderBy(o => o.SiteName).ToListAsync();
        var firm = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == firmSlug);
        return obs.Select(o => ToDto(o, firm?.Name ?? firmSlug)).ToList();
    }

    public async Task<SiteObligationDto?> GetObligationBySiteAsync(Guid siteId)
    {
        var o = await db.SiteObligations.Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.SiteId == siteId);
        if (o is null) return null;
        var firm = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == o.FirmSlug);
        return ToDto(o, firm?.Name ?? o.FirmSlug);
    }

    public async Task<SiteObligationDto> CreateObligationAsync(
        Guid siteId, string siteName, SiteType siteType,
        string firmSlug, int daireCount, int blokCount, int kisimCount)
    {
        var tiers = await db.SiteBillingTiers.OrderBy(t => t.DisplayOrder).ToListAsync();
        var monthly = CalculateFromTiers(tiers, daireCount);

        var existing = await db.SiteObligations.FirstOrDefaultAsync(o => o.SiteId == siteId);
        if (existing is not null)
        {
            existing.SiteName = siteName; existing.SiteType = siteType;
            existing.DaireCount = daireCount; existing.BlokCount = blokCount;
            existing.KisimCount = kisimCount; existing.MonthlyAmount = monthly;
            existing.IsActive = true;
            await db.SaveChangesAsync();
            var firm2 = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == firmSlug);
            return ToDto(existing, firm2?.Name ?? firmSlug);
        }

        var ob = new SiteObligation
        {
            FirmSlug = firmSlug, SiteId = siteId, SiteName = siteName, SiteType = siteType,
            DaireCount = daireCount, BlokCount = blokCount, KisimCount = kisimCount,
            MonthlyAmount = monthly, BillingPeriod = BillingPeriod.Monthly,
            PricePerDaire = 0, PricePerBlok = 0, PricePerKisim = 0
        };
        db.SiteObligations.Add(ob);
        await db.SaveChangesAsync();

        var firm = await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == firmSlug);
        return ToDto(ob, firm?.Name ?? firmSlug);
    }

    public async Task UpdateObligationCountsAsync(Guid siteId, int daireCount, int blokCount, int kisimCount)
    {
        var ob = await db.SiteObligations.FirstOrDefaultAsync(o => o.SiteId == siteId);
        if (ob is null) return;
        var tiers = await db.SiteBillingTiers.OrderBy(t => t.DisplayOrder).ToListAsync();
        ob.DaireCount = daireCount; ob.BlokCount = blokCount; ob.KisimCount = kisimCount;
        ob.MonthlyAmount = CalculateFromTiers(tiers, daireCount);
        await db.SaveChangesAsync();
    }

    public async Task DeactivateObligationAsync(Guid siteId)
    {
        var ob = await db.SiteObligations.FirstOrDefaultAsync(o => o.SiteId == siteId);
        if (ob is null) return;
        ob.IsActive = false;
        await db.SaveChangesAsync();
    }

    // ── Ödemeler ─────────────────────────────────────────────────────────────

    public async Task<List<ObligationPaymentDto>> GetPaymentsAsync(int obligationId)
    {
        var ob = await db.SiteObligations.FindAsync(obligationId);
        var firm = ob is not null ? await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == ob.FirmSlug) : null;
        var firmName = firm?.Name ?? ob?.FirmSlug ?? "";

        return await db.SiteObligationPayments
            .Include(p => p.Obligation)
            .Where(p => p.ObligationId == obligationId)
            .OrderByDescending(p => p.PeriodStart)
            .Select(p => ToPaymentDto(p, p.Obligation.SiteName, p.Obligation.FirmSlug, firmName))
            .ToListAsync();
    }

    public async Task<List<ObligationPaymentDto>> GetAllPendingPaymentsAsync()
    {
        var firmNames = await db.FirmRegistrations.ToDictionaryAsync(f => f.Slug, f => f.Name);
        return await db.SiteObligationPayments
            .Include(p => p.Obligation)
            .Where(p => p.Status == ObligationPaymentStatus.Pending || p.Status == ObligationPaymentStatus.Overdue)
            .OrderBy(p => p.DueDate)
            .Select(p => ToPaymentDto(p, p.Obligation.SiteName, p.Obligation.FirmSlug,
                firmNames.ContainsKey(p.Obligation.FirmSlug) ? firmNames[p.Obligation.FirmSlug] : p.Obligation.FirmSlug))
            .ToListAsync();
    }

    public async Task<ObligationPaymentDto> UpsertPaymentAsync(int? existingId, ObligationPaymentCommand cmd)
    {
        SiteObligationPayment payment;
        if (existingId.HasValue)
            payment = await db.SiteObligationPayments.FindAsync(existingId.Value) ?? throw new KeyNotFoundException();
        else { payment = new SiteObligationPayment(); db.SiteObligationPayments.Add(payment); }

        payment.ObligationId = cmd.ObligationId; payment.PeriodLabel = cmd.PeriodLabel;
        payment.PeriodStart = cmd.PeriodStart; payment.PeriodEnd = cmd.PeriodEnd;
        payment.AmountDue = cmd.AmountDue; payment.AmountPaid = cmd.AmountPaid;
        payment.DueDate = cmd.DueDate; payment.PaymentDate = cmd.PaymentDate;
        payment.Status = cmd.Status; payment.Notes = cmd.Notes; payment.RecordedBy = cmd.RecordedBy;
        await db.SaveChangesAsync();

        var ob = await db.SiteObligations.FindAsync(cmd.ObligationId);
        var firm = ob is not null ? await db.FirmRegistrations.FirstOrDefaultAsync(f => f.Slug == ob.FirmSlug) : null;
        return ToPaymentDto(payment, ob?.SiteName ?? "", ob?.FirmSlug ?? "", firm?.Name ?? ob?.FirmSlug ?? "");
    }

    public async Task DeletePaymentAsync(int id)
    {
        var p = await db.SiteObligationPayments.FindAsync(id) ?? throw new KeyNotFoundException();
        db.SiteObligationPayments.Remove(p);
        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SiteObligationDto ToDto(SiteObligation o, string firmName)
    {
        var pending  = o.Payments.Count(p => p.Status is ObligationPaymentStatus.Pending or ObligationPaymentStatus.Overdue);
        var overdue  = o.Payments.Count(p => p.Status == ObligationPaymentStatus.Overdue);
        var totalDue = o.Payments.Sum(p => p.AmountDue);
        var totalPaid= o.Payments.Sum(p => p.AmountPaid);
        return new(o.Id, o.FirmSlug, firmName, o.SiteId, o.SiteName, o.SiteType,
            o.DaireCount, o.BlokCount, o.KisimCount, o.MonthlyAmount, o.BillingPeriod,
            o.IsActive, o.CreatedAt, pending, overdue, totalDue, totalPaid);
    }

    private static ObligationPaymentDto ToPaymentDto(SiteObligationPayment p, string siteName, string firmSlug, string firmName) =>
        new(p.Id, p.ObligationId, siteName, firmSlug,
            p.PeriodLabel, p.PeriodStart, p.PeriodEnd,
            p.AmountDue, p.AmountPaid, p.DueDate, p.PaymentDate,
            p.Status, p.Notes, p.RecordedBy);
}
