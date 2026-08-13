using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database;

public sealed class MyosotisDbContextFactory : IDesignTimeDbContextFactory<MyosotisDbContext>
{
    public MyosotisDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MyosotisDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new MyosotisDbContext(options);
    }
}
