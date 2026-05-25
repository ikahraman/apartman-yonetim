using ApartmanYonetim.Domain.Enums;
namespace ApartmanYonetim.Application.Services;

public record MeetingDto(Guid Id, string Title, string? Description, DateTime MeetingDate, string? Location, MeetingType MeetingType, MeetingStatus Status, string? AgendaItems, string CreatedBy, bool HasMinutes);
public record MeetingMinutesDto(Guid Id, Guid MeetingId, string Content, int AttendeeCount, string? Decisions, DateTime CreatedAt);
public record MeetingCommand(string Title, string? Description, DateTime MeetingDate, string? Location, MeetingType MeetingType, string? AgendaItems);
public record MeetingMinutesCommand(string Content, int AttendeeCount, string? Decisions);

public interface ISiteMeetingService
{
    Task<List<MeetingDto>> GetAllAsync(string dbFilePath);
    Task<MeetingDto> AddAsync(string dbFilePath, MeetingCommand cmd, string createdBy);
    Task UpdateAsync(string dbFilePath, Guid id, MeetingCommand cmd);
    Task UpdateStatusAsync(string dbFilePath, Guid id, MeetingStatus status);
    Task<MeetingMinutesDto?> GetMinutesAsync(string dbFilePath, Guid meetingId);
    Task SaveMinutesAsync(string dbFilePath, Guid meetingId, MeetingMinutesCommand cmd);
}
