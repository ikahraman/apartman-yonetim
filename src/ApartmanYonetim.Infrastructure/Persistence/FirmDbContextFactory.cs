using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class FirmDbContextFactory(string baseDirectory = "FirmDatabases")
{
    public string BaseDirectory { get; } = baseDirectory;

    public string ResolvePath(string firmSlug)
        => Path.Combine(BaseDirectory, firmSlug, "firm.db");

    public FirmDbContext Create(string dbFilePath)
    {
        var opts = new DbContextOptionsBuilder<FirmDbContext>()
            .UseSqlite($"Data Source={dbFilePath};Cache=Shared;Mode=ReadWriteCreate")
            .Options;
        return new FirmDbContext(opts);
    }

    public FirmDbContext CreateBySlug(string firmSlug)
        => Create(ResolvePath(firmSlug));

    public async Task<FirmDbContext> CreateAndMigrateAsync(string firmSlug)
    {
        var path = ResolvePath(firmSlug);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var db = Create(path);
        await db.Database.MigrateAsync();
        // WAL modu dosya seviyesinde kalıcı — ilk oluşturmada bir kere yeterli
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        return db;
    }
}
