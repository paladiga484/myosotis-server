using System.Text.Json;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class StoryDungeonRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<StoryDungeonRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The run in progress, or null.</summary>
    public async Task<StoryDungeonSaveInfoFormat?> GetSaveAsync(long uid)
    {
        var row = await Db.StoryDungeonSaves.AsNoTracking().FirstOrDefaultAsync(x => x.Uid == uid);
        if (row is null || string.IsNullOrEmpty(row.SaveJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<StoryDungeonSaveInfoFormat>(row.SaveJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Corrupt story dungeon save for uid {Uid}; treating as absent", uid);
            return null;
        }
    }

    public async Task<bool> StoreSaveAsync(long uid, StoryDungeonSaveInfoFormat save)
    {
        var row = await RowAsync(uid);
        row.DungeonId = save.dungeonid;
        row.SaveJson = JsonSerializer.Serialize(save, JsonOptions);
        row.UpdatedAt = Common.TimeUtil.UnixNow();
        return await SaveAsync(uid);
    }

    /// <summary>Ends the run; the node records stay.</summary>
    public async Task<bool> ClearSaveAsync(long uid)
    {
        var row = await Db.StoryDungeonSaves.FirstOrDefaultAsync(x => x.Uid == uid);
        if (row is null)
            return true;
        row.SaveJson = "";
        row.DungeonId = 0;
        row.UpdatedAt = Common.TimeUtil.UnixNow();
        return await SaveAsync(uid);
    }

    /// <summary>The nodes this user has cleared in a dungeon, ascending.</summary>
    public async Task<List<int>> GetRecordAsync(long uid, int dungeonId)
    {
        var row = await Db.StoryDungeonSaves.AsNoTracking().FirstOrDefaultAsync(x => x.Uid == uid);
        var records = Records(row?.RecordsJson);
        return records.TryGetValue(dungeonId, out var nodes) ? nodes.Order().ToList() : [];
    }

    public async Task<bool> AddRecordAsync(long uid, int dungeonId, int nodeId)
    {
        var row = await RowAsync(uid);
        var records = Records(row.RecordsJson);
        var nodes = records.TryGetValue(dungeonId, out var n) ? n : records[dungeonId] = [];
        if (nodes.Contains(nodeId))
            return true;
        nodes.Add(nodeId);
        row.RecordsJson = JsonSerializer.Serialize(records, JsonOptions);
        return await SaveAsync(uid);
    }

    private async Task<StoryDungeonSave> RowAsync(long uid)
    {
        var row = await Db.StoryDungeonSaves.FirstOrDefaultAsync(x => x.Uid == uid);
        if (row is null)
        {
            row = new StoryDungeonSave { Uid = uid };
            Db.StoryDungeonSaves.Add(row);
        }
        return row;
    }

    private Dictionary<int, List<int>> Records(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, List<int>>>(json, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Corrupt story dungeon records; starting them afresh");
            return [];
        }
    }
}
