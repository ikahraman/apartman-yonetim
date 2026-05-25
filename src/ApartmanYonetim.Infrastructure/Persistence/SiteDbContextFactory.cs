using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class SiteDbContextFactory(string baseDirectory = "SiteDatabases")
{
    public string BaseDirectory { get; } = baseDirectory;

    public string ResolvePath(string dbFilePath)
        => Path.IsPathRooted(dbFilePath) ? dbFilePath : Path.Combine(BaseDirectory, Path.GetFileName(dbFilePath));

    public SiteDbContext Create(string dbFilePath)
    {
        var resolved = ResolvePath(dbFilePath);
        var opts = new DbContextOptionsBuilder<SiteDbContext>()
            .UseSqlite($"Data Source={resolved}")
            .Options;
        return new SiteDbContext(opts);
    }

    public async Task<SiteDbContext> CreateAndMigrateAsync(string dbFilePath)
    {
        var resolved = ResolvePath(dbFilePath);
        Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
        var db = Create(dbFilePath);
        await db.Database.MigrateAsync();
        return db;
    }
}
