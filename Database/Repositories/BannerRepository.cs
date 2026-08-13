using Common;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class BannerRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<BannerRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<List<UserBannerDataFormat>> ListAsync(long uid)
    {
        var rows = await Db.UserBanners.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.Id)
            .ToListAsync();
        return rows.Select(r => new UserBannerDataFormat
        {
            id = r.Id,
            acquiretime = TimeUtil.ToIso(r.Acquiretime),
            value = r.Value,
            value2 = r.Value2,
            value3 = r.Value3,
            value4 = r.Value4,
            value5 = r.Value5,
        }).ToList();
    }

    public async Task<HashSet<int>> GetIdsAsync(long uid) =>
        (await Db.UserBanners.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => x.Id)
            .ToListAsync()).ToHashSet();

    public async Task SyncNewAsync(long uid, IReadOnlyCollection<int> allIds)
    {
        var existing = await GetIdsAsync(uid);
        var missing = allIds.Where(id => !existing.Contains(id)).OrderBy(id => id).ToList();
        if (missing.Count == 0)
            return;

        var now = TimeUtil.UnixNow();
        Db.UserBanners.AddRange(missing.Select(id =>
        {
            var (value, value2) = SeedValues.BannerValue(id);
            return new UserBanner { Uid = uid, Id = id, Acquiretime = now, Value = value, Value2 = value2 };
        }));
        await SaveAsync(uid);
    }
}
