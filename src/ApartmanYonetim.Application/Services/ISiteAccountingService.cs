using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record AccountingEntryDto(Guid Id, AccountingEntryType Type, string Category, decimal Amount, DateOnly Date, string Description, Guid? UnitId, string? UnitNumber, string CreatedBy, DateTime CreatedAt);
public record AccountingCommand(AccountingEntryType Type, string Category, decimal Amount, DateOnly Date, string Description, Guid? UnitId);
public record AccountingSummaryDto(decimal TotalIncome, decimal TotalExpense, decimal Balance);

public interface ISiteAccountingService
{
    Task<List<AccountingEntryDto>> GetAllAsync(string dbFilePath, int? year = null, int? month = null);
    Task<AccountingEntryDto> AddAsync(string dbFilePath, AccountingCommand cmd, string createdBy);
    Task DeleteAsync(string dbFilePath, Guid id);
    Task<AccountingSummaryDto> GetSummaryAsync(string dbFilePath, int? year = null, int? month = null);
}
