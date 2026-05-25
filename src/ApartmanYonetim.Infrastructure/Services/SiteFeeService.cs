using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteFeeService(SiteDbContextFactory factory) : ISiteFeeService
{
    private static readonly string[] Months = ["Ocak","Şubat","Mart","Nisan","Mayıs","Haziran","Temmuz","Ağustos","Eylül","Ekim","Kasım","Aralık"];
    private static string PeriodLabel(int year, int month) => $"{Months[month - 1]} {year}";

    public async Task<List<FeeScheduleDto>> GetSchedulesAsync(string dbFilePath)
    {
        await using var db = factory.Create(dbFilePath);
        return await db.FeeSchedules.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name)
            .Select(s => new FeeScheduleDto(s.Id, s.Name, s.Amount, s.Period, s.DueDay, s.StartDate, s.EndDate, s.IsActive))
            .ToListAsync();
    }

    public async Task<FeeScheduleDto> AddScheduleAsync(string dbFilePath, FeeScheduleCommand cmd)
    {
        await using var db = factory.Create(dbFilePath);
        var s = new SiteFeeSchedule { Name = cmd.Name, Amount = cmd.Amount, Period = cmd.Period, DueDay = cmd.DueDay, StartDate = cmd.StartDate, EndDate = cmd.EndDate };
        db.FeeSchedules.Add(s);
        await db.SaveChangesAsync();
        return new FeeScheduleDto(s.Id, s.Name, s.Amount, s.Period, s.DueDay, s.StartDate, s.EndDate, s.IsActive);
    }

    public async Task UpdateScheduleAsync(string dbFilePath, Guid scheduleId, FeeScheduleCommand cmd)
    {
        await using var db = factory.Create(dbFilePath);
        var s = await db.FeeSchedules.FindAsync(scheduleId) ?? throw new InvalidOperationException("Aidat planı bulunamadı.");
        s.Name = cmd.Name; s.Amount = cmd.Amount; s.Period = cmd.Period;
        s.DueDay = cmd.DueDay; s.StartDate = cmd.StartDate; s.EndDate = cmd.EndDate;
        await db.SaveChangesAsync();
    }

    public async Task SetScheduleActiveAsync(string dbFilePath, Guid scheduleId, bool isActive)
    {
        await using var db = factory.Create(dbFilePath);
        var s = await db.FeeSchedules.FindAsync(scheduleId) ?? throw new InvalidOperationException("Aidat planı bulunamadı.");
        s.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<List<FeePaymentDto>> GetPaymentsAsync(string dbFilePath, Guid scheduleId, int year, int month)
    {
        await using var db = factory.Create(dbFilePath);
        var label = PeriodLabel(year, month);
        var payments = await db.FeePayments.Where(p => p.ScheduleId == scheduleId && p.PeriodLabel == label).ToListAsync();
        var unitIds = payments.Select(p => p.UnitId).Distinct().ToList();
        var units = await db.Units.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);
        return payments
            .OrderBy(p => units.TryGetValue(p.UnitId, out var u) ? u.Block : null)
            .ThenBy(p => units.TryGetValue(p.UnitId, out var u) ? u.Number : null)
            .Select(p =>
            {
                units.TryGetValue(p.UnitId, out var unit);
                return new FeePaymentDto(p.Id, p.UnitId, unit?.Number ?? "?", unit?.Block, p.ScheduleId, p.PeriodLabel, p.DueDate, p.Amount, p.PaidDate, p.PaidAmount, p.Status, p.Notes);
            }).ToList();
    }

    public async Task<List<FeePaymentDto>> GetPaymentsByUnitAsync(string dbFilePath, Guid unitId, int count = 12)
    {
        await using var db = factory.Create(dbFilePath);
        var unit = await db.Units.FindAsync(unitId);
        var payments = await db.FeePayments.Where(p => p.UnitId == unitId).OrderByDescending(p => p.DueDate).Take(count).ToListAsync();
        return payments.Select(p => new FeePaymentDto(p.Id, p.UnitId, unit?.Number ?? "?", unit?.Block, p.ScheduleId, p.PeriodLabel, p.DueDate, p.Amount, p.PaidDate, p.PaidAmount, p.Status, p.Notes)).ToList();
    }

    public async Task GeneratePaymentsForMonthAsync(string dbFilePath, Guid scheduleId, int year, int month)
    {
        await using var db = factory.Create(dbFilePath);
        var schedule = await db.FeeSchedules.FindAsync(scheduleId) ?? throw new InvalidOperationException("Aidat planı bulunamadı.");
        var label = PeriodLabel(year, month);
        if (await db.FeePayments.AnyAsync(p => p.ScheduleId == scheduleId && p.PeriodLabel == label))
            throw new InvalidOperationException($"'{label}' için kayıt zaten mevcut.");
        var units = await db.Units.ToListAsync();
        var dueDate = new DateOnly(year, month, Math.Min(schedule.DueDay, DateTime.DaysInMonth(year, month)));
        db.FeePayments.AddRange(units.Select(u => new SiteFeePayment
        {
            UnitId = u.Id, ScheduleId = scheduleId, PeriodLabel = label,
            DueDate = dueDate, Amount = schedule.Amount, Status = FeePaymentStatus.Pending
        }));
        await db.SaveChangesAsync();
    }

    public async Task RecordPaymentAsync(string dbFilePath, Guid paymentId, decimal amount, DateOnly paidDate, string? notes)
    {
        await using var db = factory.Create(dbFilePath);
        var p = await db.FeePayments.FindAsync(paymentId) ?? throw new InvalidOperationException("Kayıt bulunamadı.");
        p.PaidAmount = amount; p.PaidDate = paidDate; p.Notes = notes;
        p.Status = amount >= p.Amount ? FeePaymentStatus.Paid : FeePaymentStatus.Partial;
        p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<(decimal TotalExpected, decimal TotalCollected, int PaidCount, int PendingCount)> GetMonthSummaryAsync(string dbFilePath, Guid scheduleId, int year, int month)
    {
        await using var db = factory.Create(dbFilePath);
        var label = PeriodLabel(year, month);
        var payments = await db.FeePayments.Where(p => p.ScheduleId == scheduleId && p.PeriodLabel == label).ToListAsync();
        return (payments.Sum(p => p.Amount), payments.Sum(p => p.PaidAmount ?? 0),
            payments.Count(p => p.Status == FeePaymentStatus.Paid),
            payments.Count(p => p.Status is FeePaymentStatus.Pending or FeePaymentStatus.Overdue));
    }
}
