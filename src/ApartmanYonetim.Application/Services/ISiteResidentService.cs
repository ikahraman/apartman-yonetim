using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record UnitSummaryDto(
    Guid Id, string Number, string? Block, int? Floor, decimal? SquareMeters,
    OccupancyType OccupancyType,
    Guid? ResidentId, string? ResidentName, string? ResidentPhone,
    ResidencyType? ResidencyType, string? ResidentEmail = null,
    UnitType UnitType = UnitType.Daire, decimal? ArsaPay = null,
    Guid? BlockId = null, string? BlockName = null);

public record SiteKisimDto(Guid Id, string Name, string? Code, string? Description);
public record SiteKisimCommand(string Name, string? Code, string? Description);
public record SiteBlockDto(Guid Id, string Name, string? Code, int? FloorCount, int? UnitCount, Guid? KisimId = null, string? KisimName = null);
public record SiteBlockCommand(string Name, string? Code, int? FloorCount, int? UnitCount, Guid? KisimId = null);

public record ResidentDto(
    Guid Id, Guid UnitId, string FirstName, string LastName, string FullName,
    string? Phone, string? Email, ResidencyType ResidencyType,
    DateOnly MoveInDate, DateOnly? MoveOutDate, bool IsActive, string? Notes);

public record AddUnitCommand(string Number, string? Block, int? Floor, decimal? SquareMeters,
    UnitType UnitType = UnitType.Daire, decimal? ArsaPay = null, Guid? BlockId = null);
public record ResidentFormCommand(string FirstName, string LastName, string? Phone, string? Email, ResidencyType ResidencyType, DateOnly MoveInDate, string? Notes);

public interface ISiteResidentService
{
    Task<List<UnitSummaryDto>> GetUnitsAsync(string dbFilePath);
    Task<UnitSummaryDto> AddUnitAsync(string dbFilePath, AddUnitCommand cmd);
    Task UpdateUnitAsync(string dbFilePath, Guid unitId, AddUnitCommand cmd);
    Task<List<ResidentDto>> GetResidentsForUnitAsync(string dbFilePath, Guid unitId);
    Task<ResidentDto> AddResidentAsync(string dbFilePath, Guid unitId, ResidentFormCommand cmd);
    Task UpdateResidentAsync(string dbFilePath, Guid residentId, ResidentFormCommand cmd);
    Task MoveOutAsync(string dbFilePath, Guid residentId, DateOnly moveOutDate);
    Task SetResidentUserIdAsync(string dbFilePath, Guid residentId, string userId);
    Task<ResidentDto?> GetByUserIdAsync(string dbFilePath, string userId);

    Task<List<SiteKisimDto>> GetKisimlarAsync(string dbFilePath);
    Task<SiteKisimDto> AddKisimAsync(string dbFilePath, SiteKisimCommand cmd);
    Task UpdateKisimAsync(string dbFilePath, Guid kisimId, SiteKisimCommand cmd);
    Task DeleteKisimAsync(string dbFilePath, Guid kisimId);

    Task<List<SiteBlockDto>> GetBlocksAsync(string dbFilePath);
    Task<SiteBlockDto> AddBlockAsync(string dbFilePath, SiteBlockCommand cmd);
    Task UpdateBlockAsync(string dbFilePath, Guid blockId, SiteBlockCommand cmd);
    Task DeleteBlockAsync(string dbFilePath, Guid blockId);
}
