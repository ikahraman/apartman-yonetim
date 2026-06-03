namespace ApartmanYonetim.Application.Services;

public record SiteOverviewDto(
    Guid SiteId, string SiteName, string DbFilePath,
    int TotalUnits, int OccupiedUnits,
    decimal CollectionRateThisMonth,
    int OpenMaintenanceCount,
    decimal YtdBalance);

public record FirmOverviewDto(
    string FirmName,
    int TotalSites, int TotalUnits, int OccupiedUnits,
    decimal AvgCollectionRate,
    int TotalOpenMaintenance,
    decimal TotalYtdBalance,
    List<SiteOverviewDto> Sites);

public interface IFirmDashboardService
{
    Task<FirmOverviewDto> GetOverviewAsync(string firmSlug);
}
