using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record FeeScheduleDto(Guid Id, string Name, decimal Amount, FeePeriod Period, int DueDay, DateOnly StartDate, DateOnly? EndDate, bool IsActive);
public record FeePaymentDto(Guid Id, Guid UnitId, string UnitNumber, string? UnitBlock, Guid ScheduleId, string PeriodLabel, DateOnly DueDate, decimal Amount, DateOnly? PaidDate, decimal? PaidAmount, FeePaymentStatus Status, string? Notes);
public record FeeScheduleCommand(string Name, decimal Amount, FeePeriod Period, int DueDay, DateOnly StartDate, DateOnly? EndDate);

public interface ISiteFeeService
{
    Task<List<FeeScheduleDto>> GetSchedulesAsync(string dbFilePath);
    Task<FeeScheduleDto> AddScheduleAsync(string dbFilePath, FeeScheduleCommand cmd);
    Task UpdateScheduleAsync(string dbFilePath, Guid scheduleId, FeeScheduleCommand cmd);
    Task SetScheduleActiveAsync(string dbFilePath, Guid scheduleId, bool isActive);
    Task<List<FeePaymentDto>> GetPaymentsAsync(string dbFilePath, Guid scheduleId, int year, int month);
    Task<List<FeePaymentDto>> GetPaymentsByUnitAsync(string dbFilePath, Guid unitId, int count = 12);
    Task GeneratePaymentsForMonthAsync(string dbFilePath, Guid scheduleId, int year, int month);
    Task RecordPaymentAsync(string dbFilePath, Guid paymentId, decimal amount, DateOnly paidDate, string? notes);
    Task<(decimal TotalExpected, decimal TotalCollected, int PaidCount, int PendingCount)> GetMonthSummaryAsync(string dbFilePath, Guid scheduleId, int year, int month);
}
