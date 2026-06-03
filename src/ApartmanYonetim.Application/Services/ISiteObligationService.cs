using ApartmanYonetim.Domain.Enums;

namespace ApartmanYonetim.Application.Services;

public record SiteBillingConfigDto(
    decimal PricePerDaire, decimal PricePerBlok, decimal PricePerKisim,
    decimal MinimumMonthly, BillingPeriod DefaultPeriod,
    DateTime UpdatedAt, string? UpdatedBy);

public record SiteBillingConfigCommand(
    decimal PricePerDaire, decimal PricePerBlok, decimal PricePerKisim,
    decimal MinimumMonthly, BillingPeriod DefaultPeriod);

public record SiteObligationDto(
    int Id, string FirmSlug, string FirmName, Guid SiteId, string SiteName, SiteType SiteType,
    int DaireCount, int BlokCount, int KisimCount,
    decimal MonthlyAmount, BillingPeriod BillingPeriod,
    bool IsActive, DateTime CreatedAt,
    int PendingCount, int OverdueCount, decimal TotalDue, decimal TotalPaid);

public record ObligationPaymentDto(
    int Id, int ObligationId, string SiteName, string FirmSlug,
    string PeriodLabel, DateOnly PeriodStart, DateOnly PeriodEnd,
    decimal AmountDue, decimal AmountPaid, DateOnly DueDate, DateOnly? PaymentDate,
    ObligationPaymentStatus Status, string? Notes, string? RecordedBy);

public record ObligationPaymentCommand(
    int ObligationId, string PeriodLabel, DateOnly PeriodStart, DateOnly PeriodEnd,
    decimal AmountDue, decimal AmountPaid,
    DateOnly DueDate, DateOnly? PaymentDate,
    ObligationPaymentStatus Status, string? Notes, string? RecordedBy);

public interface ISiteObligationService
{
    // Fiyatlandırma konfigürasyonu
    Task<SiteBillingConfigDto> GetBillingConfigAsync();
    Task UpdateBillingConfigAsync(SiteBillingConfigCommand cmd, string updatedBy);
    Task<decimal> CalculateMonthlyAsync(SiteType siteType, int daireCount, int blokCount, int kisimCount);
    Task RecalculateAllObligationsAsync();

    // Yükümlülükler
    Task<List<SiteObligationDto>> GetAllObligationsAsync();
    Task<List<SiteObligationDto>> GetObligationsByFirmAsync(string firmSlug);
    Task<SiteObligationDto?> GetObligationBySiteAsync(Guid siteId);
    Task<SiteObligationDto> CreateObligationAsync(Guid siteId, string siteName, SiteType siteType,
        string firmSlug, int daireCount, int blokCount, int kisimCount);
    Task UpdateObligationCountsAsync(Guid siteId, int daireCount, int blokCount, int kisimCount);
    Task DeactivateObligationAsync(Guid siteId);

    // Ödemeler
    Task<List<ObligationPaymentDto>> GetPaymentsAsync(int obligationId);
    Task<List<ObligationPaymentDto>> GetAllPendingPaymentsAsync();
    Task<ObligationPaymentDto> UpsertPaymentAsync(int? existingId, ObligationPaymentCommand cmd);
    Task DeletePaymentAsync(int id);
}
