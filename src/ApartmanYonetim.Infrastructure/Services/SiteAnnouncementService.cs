using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteAnnouncementService(SiteDbContextFactory factory) : ISiteAnnouncementService
{
    public async Task<List<AnnouncementDto>> GetActiveAsync(string dbFilePath)
    {
        await using var db = factory.Create(dbFilePath);
        return await db.Announcements
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > DateTime.Now)
            .OrderByDescending(a => a.IsUrgent).ThenByDescending(a => a.PublishedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Content, a.IsUrgent, a.PublishedAt, a.ExpiresAt, a.CreatedBy))
            .ToListAsync();
    }

    public async Task<List<AnnouncementDto>> GetAllAsync(string dbFilePath)
    {
        await using var db = factory.Create(dbFilePath);
        return await db.Announcements
            .OrderByDescending(a => a.IsUrgent).ThenByDescending(a => a.PublishedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Content, a.IsUrgent, a.PublishedAt, a.ExpiresAt, a.CreatedBy))
            .ToListAsync();
    }

    public async Task<AnnouncementDto> AddAsync(string dbFilePath, AnnouncementCommand cmd, string createdBy)
    {
        await using var db = factory.Create(dbFilePath);
        var a = new SiteAnnouncement { Title = cmd.Title, Content = cmd.Content, IsUrgent = cmd.IsUrgent, ExpiresAt = cmd.ExpiresAt, CreatedBy = createdBy };
        db.Announcements.Add(a);
        await db.SaveChangesAsync();
        return new AnnouncementDto(a.Id, a.Title, a.Content, a.IsUrgent, a.PublishedAt, a.ExpiresAt, a.CreatedBy);
    }

    public async Task UpdateAsync(string dbFilePath, Guid id, AnnouncementCommand cmd)
    {
        await using var db = factory.Create(dbFilePath);
        var a = await db.Announcements.FindAsync(id) ?? throw new InvalidOperationException("Duyuru bulunamadı.");
        a.Title = cmd.Title; a.Content = cmd.Content; a.IsUrgent = cmd.IsUrgent; a.ExpiresAt = cmd.ExpiresAt;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string dbFilePath, Guid id)
    {
        await using var db = factory.Create(dbFilePath);
        var a = await db.Announcements.FindAsync(id) ?? throw new InvalidOperationException("Duyuru bulunamadı.");
        db.Announcements.Remove(a);
        await db.SaveChangesAsync();
    }
}
