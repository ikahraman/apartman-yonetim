using ApartmanYonetim.Domain.Enums;

namespace ApartmanYonetim.Application.Services;

public record PackageDto(int Id, string Name, int MinSiteCount, int? MaxSiteCount, decimal MonthlyPrice, bool IsActive, int DisplayOrder);
public record PackageCommand(string Name, int MinSiteCount, int? MaxSiteCount, decimal MonthlyPrice, int DisplayOrder);

public record SubscriptionDto(
    int Id, string FirmSlug, string FirmName,
    int FirmPackageId, string PackageName, int? MaxSiteCount,
    DateOnly ContractStartDate, DateOnly? ContractEndDate,
    decimal? CustomMonthlyPrice, decimal EffectiveMonthlyPrice,
    SubscriptionStatus Status, string? Notes, string? LastModifiedBy, DateTime? LastModifiedAt,
    int ActiveSiteCount);

public record SubscriptionCommand(
    string FirmSlug, int FirmPackageId,
    DateOnly ContractStartDate, DateOnly? ContractEndDate,
    decimal? CustomMonthlyPrice, SubscriptionStatus Status, string? Notes);

public record PaymentRecordDto(
    int Id, string FirmSlug, int PeriodYear, int PeriodMonth,
    decimal AmountDue, decimal AmountPaid,
    DateOnly DueDate, DateOnly? PaymentDate,
    PaymentRecordStatus PaymentStatus, string? Notes);

public record PaymentRecordCommand(
    string FirmSlug, int PeriodYear, int PeriodMonth,
    decimal AmountDue, decimal AmountPaid,
    DateOnly DueDate, DateOnly? PaymentDate,
    PaymentRecordStatus PaymentStatus, string? Notes);

public interface IFirmSubscriptionService
{
    // Paketler
    Task<List<PackageDto>> GetPackagesAsync();
    Task<PackageDto?> GetPackageByIdAsync(int id);
    Task<PackageDto> CreatePackageAsync(PackageCommand cmd);
    Task UpdatePackageAsync(int id, PackageCommand cmd);
    Task SetPackageActiveAsync(int id, bool isActive);

    // Abonelikler
    Task<List<SubscriptionDto>> GetAllSubscriptionsAsync();
    Task<SubscriptionDto?> GetSubscriptionByFirmAsync(string firmSlug);
    Task<SubscriptionDto> CreateSubscriptionAsync(SubscriptionCommand cmd, string modifiedBy);
    Task UpdateSubscriptionAsync(int id, SubscriptionCommand cmd, string modifiedBy);

    // Ödeme kayıtları
    Task<List<PaymentRecordDto>> GetPaymentRecordsAsync(string firmSlug);
    Task<PaymentRecordDto> UpsertPaymentRecordAsync(PaymentRecordCommand cmd);
    Task DeletePaymentRecordAsync(int id);

    // Limit kontrolü
    Task<(bool allowed, int currentCount, int? maxCount)> CheckSiteLimitAsync(string firmSlug);
}
