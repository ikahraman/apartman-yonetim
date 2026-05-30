namespace ApartmanYonetim.Application.Services;

public interface ISiteReportService
{
    Task<byte[]> ExportFeeCollectionAsync(string dbFilePath, int year, int month);
    Task<byte[]> ExportAccountingSummaryAsync(string dbFilePath, int year);
}
