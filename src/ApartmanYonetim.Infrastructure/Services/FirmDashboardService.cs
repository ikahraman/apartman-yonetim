using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApartmanYonetim.Infrastructure.Services;

public class FirmDashboardService(
    FirmDbContext firmDb,
    SiteDbContextFactory siteFactory,
    MainDbContext mainDb) : IFirmDashboardService
{
    public async Task<FirmOverviewDto> GetOverviewAsync(string firmSlug)
    {
        var company = await firmDb.Companies.FirstOrDefaultAsync();
        var sites = await firmDb.Sites.Where(s => s.IsActive).ToListAsync();

        var now = DateTime.Now;
        var siteOverviews = new List<SiteOverviewDto>();

        foreach (var site in sites)
        {
            try
            {
                await using var sdb = siteFactory.Create(site.DbFilePath);

                var totalUnits = await sdb.Units.CountAsync();
                var occupiedUnits = await sdb.Units.CountAsync(u => u.OccupancyType != OccupancyType.Empty);

                // Bu ay tahsilat
                var paymentsThisMonth = await sdb.FeePayments
                    .Where(p => p.DueDate.Year == now.Year && p.DueDate.Month == now.Month)
                    .ToListAsync();
                var totalDue = paymentsThisMonth.Sum(p => p.Amount);
                var totalPaid = paymentsThisMonth.Where(p => p.Status == FeePaymentStatus.Paid || p.Status == FeePaymentStatus.Partial)
                    .Sum(p => p.PaidAmount ?? 0);
                var collectionRate = totalDue > 0 ? (totalPaid / totalDue) * 100m : 0m;

                // Açık arıza
                var openMaintenance = await sdb.MaintenanceRequests
                    .CountAsync(m => m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress);

                // YTD bakiye
                var ytdIncome = await sdb.AccountingEntries
                    .Where(e => e.Date.Year == now.Year && e.Type == AccountingEntryType.Income)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0;
                var ytdExpense = await sdb.AccountingEntries
                    .Where(e => e.Date.Year == now.Year && e.Type == AccountingEntryType.Expense)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0;

                siteOverviews.Add(new SiteOverviewDto(
                    site.Id, site.Name, site.DbFilePath,
                    totalUnits, occupiedUnits,
                    Math.Round(collectionRate, 1),
                    openMaintenance,
                    ytdIncome - ytdExpense));
            }
            catch { /* site DB erişilemiyorsa atla */ }
        }

        var totalOccupied = siteOverviews.Sum(s => s.OccupiedUnits);
        var totalUnitsAll = siteOverviews.Sum(s => s.TotalUnits);
        var avgCollection = siteOverviews.Count > 0 ? siteOverviews.Average(s => s.CollectionRateThisMonth) : 0;

        return new FirmOverviewDto(
            company?.Name ?? firmSlug,
            siteOverviews.Count,
            totalUnitsAll,
            totalOccupied,
            Math.Round(avgCollection, 1),
            siteOverviews.Sum(s => s.OpenMaintenanceCount),
            siteOverviews.Sum(s => s.YtdBalance),
            siteOverviews);
    }
}
