using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class SiteDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SiteDbContext>
{
    public SiteDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<SiteDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new SiteDbContext(opts);
    }
}
