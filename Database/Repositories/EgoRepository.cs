using Common;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class EgoRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<EgoRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<List<EgoFormat>> ListAsync(long uid)
    {
        var rows = await Db.UserEgos.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.EgoId)
            .ToListAsync();
        return rows.Select(ToWire).ToList();
    }

    public async Task<bool> UpdateAsync(long uid, int egoId, int? gacksung = null)
    {
        var row = await Db.UserEgos.FirstOrDefaultAsync(x => x.Uid == uid && x.EgoId == egoId);
        if (row is null)
            return true;

        if (gacksung is not null) row.Gacksung = gacksung.Value;
        return await SaveAsync(uid);
    }

    public async Task<HashSet<int>> GetIdsAsync(long uid) =>
        (await Db.UserEgos.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => x.EgoId)
            .ToListAsync()).ToHashSet();

    public async Task<List<EgoFormat>> SyncNewAsync(long uid, IReadOnlyCollection<int> allIds)
    {
        var existing = await GetIdsAsync(uid);
        var missing = allIds.Where(id => !existing.Contains(id)).OrderBy(id => id).ToList();
        if (missing.Count == 0)
            return [];

        var now = TimeUtil.UnixNow();
        Db.UserEgos.AddRange(missing.Select(id => new UserEgo { Uid = uid, EgoId = id, Gacksung = SeedValues.EgoGacksung, AcquireTime = now }));
        await SaveAsync(uid);

        return missing.Select(id => new EgoFormat
        {
            ego_id = id,
            gacksung = SeedValues.EgoGacksung,
            acquire_time = TimeUtil.ToIso(now),
        }).ToList();
    }

    private static EgoFormat ToWire(UserEgo r) => new()
    {
        ego_id = r.EgoId,
        gacksung = r.Gacksung,
        acquire_time = TimeUtil.ToIso(r.AcquireTime),
    };
}
