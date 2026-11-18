using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Resource;

// ── the slices of static data the generator needs ────────────────────────────────

/// <summary>Per-floor generation weights from <c>mirrordungeon/mirrordungeon-NN*.json</c>.</summary>
public sealed class MdFloorConfig
{
    /// <summary>Floor index. Confusingly named <c>count</c> in the data; it is NOT a node count.</summary>
    public int count { get; set; }
    public double eventNodePercentage { get; set; }
    public double abnormalityBattlePercentage { get; set; }
    public double hardBattlePercentage { get; set; }
    public double hardAbBattlePercentage { get; set; }
    public List<MdFixedEncounter> fixedEncounters { get; set; } = [];
    public List<int> enemyBuffPool { get; set; } = [];
}

/// <summary>A node the floor always contains, e.g. the rest stop before the boss.</summary>
public sealed class MdFixedEncounter
{
    /// <summary>Negative values count back from the end: -2 is the step before the last.</summary>
    public int targetSector { get; set; }
    public int e { get; set; }
    public int eid { get; set; }
}

public sealed class MdDungeonConfig
{
    public int id { get; set; }
    public int typeIndex { get; set; }
    public List<int> basicEnemyLevelPerFloor { get; set; } = [];
    public int startCost { get; set; }
    public int winFloorIdx { get; set; }
    public List<MdFloorConfig> floors { get; set; } = [];
    public MdRewardInfo rewardInfo { get; set; } = new();
}

/// <summary>What a finished run pays out, keyed by how far the player got.</summary>
public sealed class MdRewardInfo
{
    public List<MdExitReward> exitRewardListByCondition { get; set; } = [];
}

public sealed class MdExitReward
{
    public int rewardId { get; set; }
    public MdExitRewardCondition condition { get; set; } = new();
    public MdExitRewardConsumption consumption { get; set; } = new();
    public List<MdRewardElement> rewardElements { get; set; } = [];
}

public sealed class MdExitRewardCondition
{
    /// <summary>Highest floor index that must be cleared. -1 is the "got nowhere" row.</summary>
    public int clearedfloorIndex { get; set; }
}

public sealed class MdExitRewardConsumption
{
    public int bonusChanceConsumption { get; set; }
    public int hardChanceConsumption { get; set; }
    public int moduleConsumption { get; set; }
}

public sealed class MdRewardElement
{
    public string type { get; set; } = "";
    public int id { get; set; }
    public int num { get; set; }
}

/// <summary>The encounter pools a chosen theme floor draws from.</summary>
public sealed class MdMapGenOption
{
    public List<int> bossPool { get; set; } = [];
    public List<int> battlePool { get; set; } = [];
    public List<int> abBattlePool { get; set; } = [];
    public List<int> hardBattlePool { get; set; } = [];
    public List<int> hardAbBattlePool { get; set; } = [];
    public List<int> eventPool { get; set; } = [];
}

public sealed class MdMapGenStep
{
    public string type { get; set; } = "";
    public List<int> numberList { get; set; } = [];
    public int numberValue { get; set; }
}

public sealed class MdThemeFloor
{
    public int id { get; set; }
    public MdMapGenOption mapGenOption { get; set; } = new();
    public List<int> egoGiftPool { get; set; } = [];
    public List<MdMapGenStep> mapGenSequence { get; set; } = [];

    /// <summary>
    /// Map shape from <c>CREATE_EMPTY_MD4_FLOOR</c> / <c>CREATE_FIXED_FLOOR</c>, whose
    /// numberList is (steps, width, bossSteps) - observed as (3|4|5, 3, 1).
    /// </summary>
    public (int Steps, int Width, int BossSteps) Shape
    {
        get
        {
            foreach (var step in mapGenSequence)
            {
                if (step.type is not ("CREATE_EMPTY_MD4_FLOOR" or "CREATE_FIXED_FLOOR"))
                    continue;
                if (step.numberList.Count >= 3)
                    return (step.numberList[0], step.numberList[1], step.numberList[2]);
            }
            return (4, 3, 1);
        }
    }
}

// ── loader ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Loads the Mirror Dungeon generator inputs out of the static data.
///
/// Note for anyone extending this: <c>mirrordungeon-generatenodepool</c> looks like the node
/// source but every one of its 1831 entries is <c>dungeonId: 3</c>, i.e. the older Mirror
/// Dungeon. For dungeon 7 the encounter ids come from each theme floor's
/// <c>mapGenOption</c> pools, which is what this loads.
/// </summary>
public sealed class MirrorDungeonData(string staticDataRoot, ILogger<MirrorDungeonData> logger)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private Dictionary<(int Id, int TypeIndex), MdDungeonConfig> _dungeons = [];
    private Dictionary<int, MdThemeFloor> _themeFloors = [];

    public IReadOnlyDictionary<(int Id, int TypeIndex), MdDungeonConfig> Dungeons => _dungeons;
    public IReadOnlyDictionary<int, MdThemeFloor> ThemeFloors => _themeFloors;

    /// <summary>
    /// All four difficulties share dungeon id 7 and are told apart by <c>typeIndex</c>:
    /// 0 normal (5 floors), 1 hard (5), 2 infinite (10), 3 extreme (15).
    /// </summary>
    public MdDungeonConfig? GetDungeon(int id, int typeIndex) =>
        _dungeons.TryGetValue((id, typeIndex), out var d) ? d
        : _dungeons.TryGetValue((id, 0), out var fallback) ? fallback
        : null;

    /// <summary>Difficulties available for a dungeon id, ascending.</summary>
    public IReadOnlyList<int> TypeIndexesFor(int id) =>
        _dungeons.Keys.Where(k => k.Id == id).Select(k => k.TypeIndex).OrderBy(t => t).ToList();

    public MdThemeFloor? GetThemeFloor(int id) =>
        _themeFloors.TryGetValue(id, out var t) ? t : null;

    public async Task LoadAllAsync()
    {
        // Every difficulty variant. They all reuse dungeon id 7, so the key has to include
        // typeIndex or the files silently overwrite each other - which is what dropped hard,
        // infinite and extreme on the first pass.
        _dungeons = await LoadAsync<MdDungeonConfig, (int, int)>("mirrordungeon", d => (d.id, d.typeIndex));
        _themeFloors = await LoadAsync<MdThemeFloor, int>("mirrordungeon-theme-floor", t => t.id);

        foreach (var ((id, typeIndex), dungeon) in _dungeons.OrderBy(kv => kv.Key))
            logger.LogInformation(
                "  mirror dungeon {Id} type {TypeIndex}: {Floors} floors, win at {Win}, cost {Cost}",
                id, typeIndex, dungeon.floors.Count, dungeon.winFloorIdx, dungeon.startCost);

        logger.LogInformation(
            "Mirror dungeon data: {Dungeons} dungeon variants, {Themes} theme floors",
            _dungeons.Count, _themeFloors.Count);
    }

    private async Task<Dictionary<TKey, T>> LoadAsync<T, TKey>(
        string dir, Func<T, TKey> keyOf, Func<string, bool>? fileFilter = null)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, T>();
        var path = Path.Combine(staticDataRoot, dir);
        if (!Directory.Exists(path))
        {
            logger.LogWarning("Mirror dungeon static data directory missing: {Path}", path);
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*.json").OrderBy(f => f))
        {
            if (fileFilter is not null && !fileFilter(Path.GetFileName(file)))
                continue;

            try
            {
                await using var stream = File.OpenRead(file);
                var wrapper = await JsonSerializer.DeserializeAsync<ListWrapper<T>>(stream, JsonOptions);
                foreach (var entry in wrapper?.list ?? [])
                    result[keyOf(entry)] = entry;
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                logger.LogWarning(ex, "Could not read {File}", file);
            }
        }

        return result;
    }

    private sealed class ListWrapper<T>
    {
        [JsonPropertyName("list")]
        public List<T> list { get; set; } = [];
    }
}
