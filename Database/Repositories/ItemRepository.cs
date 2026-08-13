using Common;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class ItemRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<ItemRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<List<ItemFormat>> ListAsync(long uid)
    {
        var rows = await Db.UserItems.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.ItemId)
            .ToListAsync();
        return rows.Select(r => new ItemFormat { item_id = r.ItemId, num = r.Num }).ToList();
    }

    public async Task<bool> UpdateAsync(long uid, int itemId, int? num = null)
    {
        var row = await Db.UserItems.FirstOrDefaultAsync(x => x.Uid == uid && x.ItemId == itemId);
        if (row is null)
            return true;

        if (num is not null) row.Num = num.Value;
        return await SaveAsync(uid);
    }

    public async Task<HashSet<int>> GetIdsAsync(long uid) =>
        (await Db.UserItems.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => x.ItemId)
            .ToListAsync()).ToHashSet();

    public async Task<List<ItemFormat>> SyncNewAsync(long uid, IReadOnlyCollection<int> allIds)
    {
        var existing = await GetIdsAsync(uid);
        var missing = allIds.Where(id => !existing.Contains(id)).OrderBy(id => id).ToList();
        if (missing.Count == 0)
            return [];

        Db.UserItems.AddRange(missing.Select(id => new UserItem { Uid = uid, ItemId = id, Num = SeedValues.ItemCount }));
        await SaveAsync(uid);

        return missing.Select(id => new ItemFormat { item_id = id, num = SeedValues.ItemCount }).ToList();
    }
}
