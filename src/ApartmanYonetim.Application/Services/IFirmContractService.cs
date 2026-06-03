using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record ContractDto(
    Guid Id, Guid SiteId, string SiteName,
    string? ContractNumber,
    DateOnly StartDate, DateOnly? EndDate,
    ContractStatus Status, ContractScope Scope,
    ManagementFeeType FeeType, decimal MonthlyFee,
    string? Notes, string? TerminationReason,
    DateTime CreatedAt, DateTime? SignedAt);

public record ContractCommand(
    Guid SiteId,
    string? ContractNumber,
    DateOnly StartDate,
    DateOnly? EndDate,
    ContractStatus Status,
    ContractScope Scope,
    ManagementFeeType FeeType,
    decimal MonthlyFee,
    string? Notes,
    DateTime? SignedAt);

public record TerminateContractCommand(string Reason);

public interface IFirmContractService
{
    Task<List<ContractDto>> GetBySiteAsync(Guid siteId);
    Task<List<ContractDto>> GetAllForFirmAsync();
    Task<ContractDto> CreateAsync(ContractCommand cmd, string createdByUserId);
    Task UpdateAsync(Guid contractId, ContractCommand cmd);
    Task TerminateAsync(Guid contractId, TerminateContractCommand cmd);
}
