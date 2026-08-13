using Common;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class PersonalityRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<PersonalityRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<List<PersonalityFormat>> ListAsync(long uid)
    {
        var rows = await Db.UserPersonalities.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.PersonalityId)
            .ToListAsync();
        return rows.Select(ToWire).ToList();
    }

    public async Task<PersonalityFormat?> GetAsync(long uid, int personalityId)
    {
        var row = await Db.UserPersonalities.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uid == uid && x.PersonalityId == personalityId);
        return row is null ? null : ToWire(row);
    }

    public async Task<bool> UpdateAsync(
        long uid, int personalityId, int? level = null, int? gacksung = null, int? gacksungIllustType = null)
    {
        var row = await Db.UserPersonalities.FirstOrDefaultAsync(x => x.Uid == uid && x.PersonalityId == personalityId);
        if (row is null)
            return true;

        if (level is not null) row.Level = level.Value;
        if (gacksung is not null) row.Gacksung = gacksung.Value;
        if (gacksungIllustType is not null) row.GacksungIllustType = gacksungIllustType.Value;
        return await SaveAsync(uid);
    }

    public async Task<HashSet<int>> GetIdsAsync(long uid) =>
        (await Db.UserPersonalities.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => x.PersonalityId)
            .ToListAsync()).ToHashSet();

    public async Task<Dictionary<int, int>> GetLevelsAsync(long uid, IReadOnlyCollection<int> personalityIds)
    {
        var ids = personalityIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var rows = await Db.UserPersonalities.AsNoTracking()
            .Where(x => x.Uid == uid && ids.Contains(x.PersonalityId))
            .Select(x => new { x.PersonalityId, x.Level })
            .ToListAsync();
        return rows.ToDictionary(r => r.PersonalityId, r => r.Level);
    }

    public async Task<List<PersonalityFormat>> SyncNewAsync(long uid, IReadOnlyCollection<int> allIds)
    {
        var existing = await GetIdsAsync(uid);
        var missing = allIds.Where(id => !existing.Contains(id)).OrderBy(id => id).ToList();
        if (missing.Count == 0)
            return [];

        var now = TimeUtil.UnixNow();
        Db.UserPersonalities.AddRange(missing.Select(id => new UserPersonality
        {
            Uid = uid,
            PersonalityId = id,
            Level = SeedValues.PersonalityLevel,
            Exp = 0,
            Gacksung = SeedValues.PersonalityGacksung,
            OrderId = ProfileUtil.GetOrderId(id),
            GacksungIllustType = SeedValues.PersonalityGacksungIllustType,
            AcquireTime = now,
        }));
        await SaveAsync(uid);

        return missing.Select(id => new PersonalityFormat
        {
            personality_id = id,
            level = SeedValues.PersonalityLevel,
            exp = 0,
            gacksung = SeedValues.PersonalityGacksung,
            order_id = ProfileUtil.GetOrderId(id),
            gacksung_illust_type = SeedValues.PersonalityGacksungIllustType,
            acquire_time = TimeUtil.ToIso(now),
        }).ToList();
    }

    private static PersonalityFormat ToWire(UserPersonality r) => new()
    {
        personality_id = r.PersonalityId,
        level = r.Level,
        exp = r.Exp,
        gacksung = r.Gacksung,
        order_id = r.OrderId,
        gacksung_illust_type = r.GacksungIllustType,
        acquire_time = TimeUtil.ToIso(r.AcquireTime),
    };
}
