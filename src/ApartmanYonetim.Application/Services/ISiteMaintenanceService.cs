using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record MaintenanceRequestDto(Guid Id, Guid? UnitId, string? UnitNumber, string? ReporterName, string? ReporterPhone, string Title, string Description, string Category, MaintenanceStatus Status, MaintenancePriority Priority, DateTime ReportedAt, string? AssignedTo, DateTime? ResolvedAt, string? ResolutionNotes);
public record MaintenanceFormCommand(Guid? UnitId, string? ReporterName, string? ReporterPhone, string Title, string Description, string Category, MaintenancePriority Priority);

public interface ISiteMaintenanceService
{
    Task<List<MaintenanceRequestDto>> GetAllAsync(string dbFilePath, MaintenanceStatus? statusFilter = null);
    Task<MaintenanceRequestDto> AddAsync(string dbFilePath, MaintenanceFormCommand cmd);
    Task UpdateStatusAsync(string dbFilePath, Guid id, MaintenanceStatus status, string? assignedTo, string? notes);
    Task ResolveAsync(string dbFilePath, Guid id, string resolutionNotes);
    Task<(int Open, int InProgress, int Resolved)> GetSummaryAsync(string dbFilePath);
}
