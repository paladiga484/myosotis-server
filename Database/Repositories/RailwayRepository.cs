using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class RailwayRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<RailwayRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<RailwayDungeonSaveInfoFormat?> GetSaveAsync(long uid, int dungeonId)
    {
        var save = await Db.RailwaySaves.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Uid == uid && x.DungeonId == dungeonId);
        if (save is null)
            return null;

        var units = await Db.RailwaySaveUnits.AsNoTracking()
            .Where(x => x.Uid == uid && x.DungeonId == dungeonId)
            .OrderBy(x => x.Pord)
            .ToListAsync();
        var egos = await Db.RailwaySaveUnitEgos.AsNoTracking()
            .Where(x => x.Uid == uid && x.DungeonId == dungeonId)
            .OrderBy(x => x.Pord)
            .ThenBy(x => x.Idx)
            .ToListAsync();

        return new RailwayDungeonSaveInfoFormat
        {
            id = save.DungeonId,
            prevclearnode = save.PrevClearNode,
            currentnode = save.CurrentNode,
            lastclearnode = save.LastClearNode,
            personalities = units.Select(u => new RailwayUnitInfoFormat
            {
                pid = u.Pid,
                g = u.G,
                l = u.L,
                es = egos.Where(e => e.Pord == u.Pord)
                    .Select(e => new DungeonEgoFormat { id = e.EgoId, g = e.Gacksung, idx = e.Idx })
                    .ToList(),
                sp = u.Sp,
                gi = u.Gi,
                sid = u.Sid,
                pord = u.Pord,
            }).ToList(),
            payreward = save.PayReward,
            rewardstate = save.RewardState,
            extrarewardstate = [],
            firstcleardate = save.FirstClearDate == 0
                ? new DateUtil(DateTimeOffset.MinValue)
                : new DateUtil(DateTimeOffset.FromUnixTimeSeconds(save.FirstClearDate)),
            currentclearrotation = save.CurrentClearRotation,
            lastenternodeid = save.LastEnterNodeId,
            lastclearrotation = save.LastClearRotation,
            buffsets = [],
            buffsetsbyegogift = [],
            initseed = save.InitSeed,
            currentseed = save.CurrentSeed,
        };
    }

    public async Task<bool> UpdateSavePersonalitiesAsync(long uid, int dungeonId, IReadOnlyList<RailwayUnitInfoFormat> units)
    {
        var save = await Db.RailwaySaves.FirstOrDefaultAsync(x => x.Uid == uid && x.DungeonId == dungeonId);
        if (save is null)
        {
            save = new RailwaySave { Uid = uid, DungeonId = dungeonId, PrevClearNode = -1, LastEnterNodeId = 1 };
            Db.RailwaySaves.Add(save);
        }

        Db.RailwaySaveUnits.RemoveRange(Db.RailwaySaveUnits.Where(x => x.Uid == uid && x.DungeonId == dungeonId));
        Db.RailwaySaveUnitEgos.RemoveRange(Db.RailwaySaveUnitEgos.Where(x => x.Uid == uid && x.DungeonId == dungeonId));

        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            Db.RailwaySaveUnits.Add(new RailwaySaveUnit
            {
                Uid = uid,
                DungeonId = dungeonId,
                Pord = i,
                Pid = unit.pid,
                G = unit.g,
                L = unit.l,
                Sp = unit.sp,
                Gi = unit.gi,
                Sid = unit.sid,
            });
            for (int j = 0; j < unit.es.Count; j++)
                Db.RailwaySaveUnitEgos.Add(new RailwaySaveUnitEgo
                {
                    Uid = uid,
                    DungeonId = dungeonId,
                    Pord = i,
                    Idx = j,
                    EgoId = unit.es[j].id,
                    Gacksung = unit.es[j].g,
                });
        }

        return await SaveAsync(uid);
    }
}
