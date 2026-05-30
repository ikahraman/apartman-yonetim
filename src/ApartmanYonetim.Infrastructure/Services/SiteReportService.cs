using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteReportService(SiteDbContextFactory factory) : ISiteReportService
{
    private static readonly string[] Months = ["Ocak","Şubat","Mart","Nisan","Mayıs","Haziran","Temmuz","Ağustos","Eylül","Ekim","Kasım","Aralık"];

    public async Task<byte[]> ExportFeeCollectionAsync(string dbFilePath, int year, int month)
    {
        await using var db = factory.Create(dbFilePath);
        var label = $"{Months[month - 1]} {year}";
        var payments = await db.FeePayments.Where(p => p.PeriodLabel == label).ToListAsync();
        var unitIds = payments.Select(p => p.UnitId).Distinct().ToList();
        var units = await db.Units.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Tahsilat {label}");

        ws.Cell(1, 1).Value = "Blok";
        ws.Cell(1, 2).Value = "Daire";
        ws.Cell(1, 3).Value = "Dönem";
        ws.Cell(1, 4).Value = "Tutar (₺)";
        ws.Cell(1, 5).Value = "Son Ödeme";
        ws.Cell(1, 6).Value = "Durum";
        ws.Cell(1, 7).Value = "Ödeme Tarihi";
        ws.Cell(1, 8).Value = "Ödenen (₺)";
        ws.Cell(1, 9).Value = "Notlar";

        var header = ws.Range(1, 1, 1, 9);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
        header.Style.Font.FontColor = XLColor.White;

        int row = 2;
        var sorted = payments
            .OrderBy(p => units.TryGetValue(p.UnitId, out var u) ? u.Block : null)
            .ThenBy(p => units.TryGetValue(p.UnitId, out var u) ? u.Number : null);

        foreach (var p in sorted)
        {
            units.TryGetValue(p.UnitId, out var unit);
            ws.Cell(row, 1).Value = unit?.Block ?? "";
            ws.Cell(row, 2).Value = unit?.Number ?? "?";
            ws.Cell(row, 3).Value = p.PeriodLabel;
            ws.Cell(row, 4).Value = (double)p.Amount;
            ws.Cell(row, 5).Value = p.DueDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 6).Value = p.Status switch { FeePaymentStatus.Paid => "Ödendi", FeePaymentStatus.Overdue => "Gecikmiş", _ => "Bekliyor" };
            ws.Cell(row, 7).Value = p.PaidDate?.ToString("dd.MM.yyyy") ?? "";
            ws.Cell(row, 8).Value = p.PaidAmount.HasValue ? (double)p.PaidAmount.Value : 0;
            ws.Cell(row, 9).Value = p.Notes ?? "";

            var statusColor = p.Status switch { FeePaymentStatus.Paid => XLColor.FromHtml("#E8F5E9"), FeePaymentStatus.Overdue => XLColor.FromHtml("#FFEBEE"), _ => XLColor.FromHtml("#FFF8E1") };
            ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = statusColor;
            row++;
        }

        var totalRow = row + 1;
        ws.Cell(totalRow, 3).Value = "TOPLAM";
        ws.Cell(totalRow, 3).Style.Font.Bold = true;
        ws.Cell(totalRow, 4).FormulaA1 = $"=SUM(D2:D{row - 1})";
        ws.Cell(totalRow, 8).FormulaA1 = $"=SUM(H2:H{row - 1})";
        ws.Range(totalRow, 1, totalRow, 9).Style.Font.Bold = true;
        ws.Range(totalRow, 1, totalRow, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportAccountingSummaryAsync(string dbFilePath, int year)
    {
        await using var db = factory.Create(dbFilePath);
        var entries = await db.AccountingEntries.Where(e => e.Date.Year == year).OrderBy(e => e.Date).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Muhasebe {year}");

        ws.Cell(1, 1).Value = "Tarih";
        ws.Cell(1, 2).Value = "Tür";
        ws.Cell(1, 3).Value = "Kategori";
        ws.Cell(1, 4).Value = "Açıklama";
        ws.Cell(1, 5).Value = "Tutar (₺)";
        ws.Cell(1, 6).Value = "Ekleyen";

        var header = ws.Range(1, 1, 1, 6);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1B5E20");
        header.Style.Font.FontColor = XLColor.White;

        int row = 2;
        foreach (var e in entries)
        {
            ws.Cell(row, 1).Value = e.Date.ToString("dd.MM.yyyy");
            ws.Cell(row, 2).Value = e.Type == AccountingEntryType.Income ? "Gelir" : "Gider";
            ws.Cell(row, 3).Value = e.Category;
            ws.Cell(row, 4).Value = e.Description;
            ws.Cell(row, 5).Value = (double)e.Amount;
            ws.Cell(row, 6).Value = e.CreatedBy;
            ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = e.Type == AccountingEntryType.Income ? XLColor.FromHtml("#F1F8E9") : XLColor.FromHtml("#FCE4EC");
            row++;
        }

        var totalRow = row + 1;
        ws.Cell(totalRow, 3).Value = "Toplam Gelir";
        ws.Cell(totalRow, 4).Value = "Toplam Gider";
        ws.Cell(totalRow, 5).Value = "Net Bakiye";
        ws.Cell(totalRow, 3).Style.Font.Bold = true;
        ws.Cell(totalRow, 4).Style.Font.Bold = true;
        ws.Cell(totalRow, 5).Style.Font.Bold = true;

        var totalRow2 = row + 2;
        var incomeTotal = entries.Where(e => e.Type == AccountingEntryType.Income).Sum(e => e.Amount);
        var expenseTotal = entries.Where(e => e.Type != AccountingEntryType.Income).Sum(e => e.Amount);
        ws.Cell(totalRow2, 3).Value = (double)incomeTotal;
        ws.Cell(totalRow2, 4).Value = (double)expenseTotal;
        ws.Cell(totalRow2, 5).Value = (double)(incomeTotal - expenseTotal);
        ws.Range(totalRow2, 3, totalRow2, 5).Style.Font.Bold = true;
        ws.Range(totalRow2, 3, totalRow2, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
