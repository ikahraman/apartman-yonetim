using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteMeetingService(SiteDbContextFactory factory) : ISiteMeetingService
{
    private static MeetingDto ToDto(SiteMeeting m) =>
        new(m.Id, m.Title, m.Description, m.MeetingDate, m.Location, m.MeetingType, m.Status, m.AgendaItems, m.CreatedBy, m.Minutes is not null);

    public async Task<List<MeetingDto>> GetAllAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        return await db.Meetings.Include(m => m.Minutes)
            .OrderByDescending(m => m.MeetingDate)
            .Select(m => ToDto(m)).ToListAsync();
    }

    public async Task<MeetingDto> AddAsync(string dbFilePath, MeetingCommand cmd, string createdBy)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var m = new SiteMeeting
        {
            Title = cmd.Title, Description = cmd.Description, MeetingDate = cmd.MeetingDate,
            Location = cmd.Location, MeetingType = cmd.MeetingType, AgendaItems = cmd.AgendaItems,
            CreatedBy = createdBy
        };
        db.Meetings.Add(m);
        await db.SaveChangesAsync();
        return ToDto(m);
    }

    public async Task UpdateAsync(string dbFilePath, Guid id, MeetingCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var m = await db.Meetings.FindAsync(id) ?? throw new InvalidOperationException("Toplantı bulunamadı.");
        m.Title = cmd.Title; m.Description = cmd.Description; m.MeetingDate = cmd.MeetingDate;
        m.Location = cmd.Location; m.MeetingType = cmd.MeetingType; m.AgendaItems = cmd.AgendaItems;
        await db.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(string dbFilePath, Guid id, MeetingStatus status)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var m = await db.Meetings.FindAsync(id) ?? throw new InvalidOperationException("Toplantı bulunamadı.");
        m.Status = status;
        await db.SaveChangesAsync();
    }

    public async Task<MeetingMinutesDto?> GetMinutesAsync(string dbFilePath, Guid meetingId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var mm = await db.MeetingMinutes.FirstOrDefaultAsync(m => m.MeetingId == meetingId);
        return mm is null ? null : new MeetingMinutesDto(mm.Id, mm.MeetingId, mm.Content, mm.AttendeeCount, mm.Decisions, mm.CreatedAt);
    }

    public async Task SaveMinutesAsync(string dbFilePath, Guid meetingId, MeetingMinutesCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var meeting = await db.Meetings.FindAsync(meetingId) ?? throw new InvalidOperationException("Toplantı bulunamadı.");
        var existing = await db.MeetingMinutes.FirstOrDefaultAsync(m => m.MeetingId == meetingId);
        if (existing is null)
        {
            db.MeetingMinutes.Add(new SiteMeetingMinutes { MeetingId = meetingId, Content = cmd.Content, AttendeeCount = cmd.AttendeeCount, Decisions = cmd.Decisions });
        }
        else
        {
            existing.Content = cmd.Content; existing.AttendeeCount = cmd.AttendeeCount; existing.Decisions = cmd.Decisions;
        }
        meeting.Status = MeetingStatus.Completed;
        await db.SaveChangesAsync();
    }
}
