using System.Text.Json;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class MirrorDungeonRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<MirrorDungeonRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The user's current run, or null if they have never entered one.</summary>
    public async Task<MirrorDungeonSaveInfoFormat?> GetSaveAsync(long uid)
    {
        var row = await Db.MirrorDungeonSaves.AsNoTracking().FirstOrDefaultAsync(x => x.Uid == uid);
        if (row is null || string.IsNullOrEmpty(row.SaveJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<MirrorDungeonSaveInfoFormat>(row.SaveJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            // A save we cannot read is worse than no save - the client would sit on a broken run
            // with no way out. Report it and let the caller start fresh.
            Logger.LogError(ex, "Corrupt mirror dungeon save for uid {Uid}; treating as absent", uid);
            return null;
        }
    }

    public async Task<bool> StoreSaveAsync(long uid, MirrorDungeonSaveInfoFormat save)
    {
        var row = await Db.MirrorDungeonSaves.FirstOrDefaultAsync(x => x.Uid == uid);
        if (row is null)
        {
            row = new MirrorDungeonSave { Uid = uid };
            Db.MirrorDungeonSaves.Add(row);
        }

        row.DungeonId = save.dungeonId;
        row.Idx = save.idx;
        row.IsEnded = save.isEndDungeon;
        row.SaveJson = JsonSerializer.Serialize(save, JsonOptions);
        row.UpdatedAt = Common.TimeUtil.UnixNow();

        return await SaveAsync(uid);
    }

    public async Task<bool> ClearSaveAsync(long uid)
    {
        var row = await Db.MirrorDungeonSaves.FirstOrDefaultAsync(x => x.Uid == uid);
        if (row is null)
            return true;

        Db.MirrorDungeonSaves.Remove(row);
        return await SaveAsync(uid);
    }
}
