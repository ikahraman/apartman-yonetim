using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class MainDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MainDbContext>
{
    public MainDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite("Data Source=design-time-main.db")
            .Options;
        return new MainDbContext(opts);
    }
}
