namespace ApartmanYonetim.Application.Services;

public record AnnouncementDto(Guid Id, string Title, string Content, bool IsUrgent, DateTime PublishedAt, DateTime? ExpiresAt, string CreatedBy);
public record AnnouncementCommand(string Title, string Content, bool IsUrgent, DateTime? ExpiresAt);

public interface ISiteAnnouncementService
{
    Task<List<AnnouncementDto>> GetActiveAsync(string dbFilePath);
    Task<List<AnnouncementDto>> GetAllAsync(string dbFilePath);
    Task<AnnouncementDto> AddAsync(string dbFilePath, AnnouncementCommand cmd, string createdBy);
    Task UpdateAsync(string dbFilePath, Guid id, AnnouncementCommand cmd);
    Task DeleteAsync(string dbFilePath, Guid id);
}
