using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ApartmanYonetim.Infrastructure.Persistence;

public class FirmDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FirmDbContext>
{
    public FirmDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<FirmDbContext>()
            .UseSqlite("Data Source=design-time-firm.db")
            .Options;
        return new FirmDbContext(opts);
    }
}
