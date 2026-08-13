using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Database.Repositories;

public abstract class RepositoryBase(MyosotisDbContext db, DatabaseService databaseService, ILogger logger)
{
    protected MyosotisDbContext Db { get; } = db;
    protected DatabaseService DatabaseService { get; } = databaseService;
    protected ILogger Logger { get; } = logger;

    protected async Task<bool> SaveAsync(long uid)
    {
        try
        {
            await Db.SaveChangesAsync();
            DatabaseService.Invalidate(uid);
            return true;
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.Sqlite.SqliteException)
        {
            Logger.LogError(ex, "Database write failed for uid {Uid}", uid);
            return false;
        }
    }
}
