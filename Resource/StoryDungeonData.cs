using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Resource;

/// <summary>A node of a Story Dungeon map, from <c>dungeonMap/*.json</c>.</summary>
public sealed class SdNode
{
    public string id { get; set; } = "";
    public string encounter { get; set; } = "";
    public int encounterId { get; set; }
    public List<SdAdjacent> adjacentNode { get; set; } = [];
}

public sealed class SdAdjacent
{
    public string nodeId { get; set; } = "";
}

public sealed class SdSector
{
    public int sectorNumber { get; set; }
    public List<SdNode> nodes { get; set; } = [];
}

public sealed class SdFloor
{
    public int count { get; set; }
    public List<SdSector> sectors { get; set; } = [];
}

public sealed class SdMap
{
    public int id { get; set; }
    public List<SdFloor> floors { get; set; } = [];

    /// <summary>(floor, sector) of a node id, or null.</summary>
    public (int Floor, int Sector)? Where(int nodeId)
    {
        var key = nodeId.ToString();
        foreach (var f in floors)
            foreach (var s in f.sectors)
                if (s.nodes.Any(n => n.id == key))
                    return (f.count, s.sectorNumber);
        return null;
    }

    /// <summary>The node a run starts on (encounter "start"), else the first node.</summary>
    public (int Floor, int Sector, int Node) Start()
    {
        foreach (var f in floors)
            foreach (var s in f.sectors)
                foreach (var n in s.nodes)
                    if (n.encounter == "start" && int.TryParse(n.id, out var id))
                        return (f.count, s.sectorNumber, id);
        var first = floors.SelectMany(f => f.sectors.SelectMany(s => s.nodes.Select(n => (f.count, s.sectorNumber, n.id))))
            .FirstOrDefault();
        return (first.count, first.sectorNumber, int.TryParse(first.id, out var fid) ? fid : 0);
    }
}

/// <summary>
/// The Story Dungeon maps (the Canto dungeons: <c>dungeon</c> entries of type STORY_DUNGEON, laid
/// out in <c>dungeonMap</c>). Unlike the Mirror Dungeon these maps are fixed: the server serves
/// them as they are.
/// </summary>
public sealed class StoryDungeonData(string staticDataRoot, ILogger<StoryDungeonData> logger)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private Dictionary<int, SdMap> _maps = [];

    public IReadOnlyDictionary<int, SdMap> Maps => _maps;

    public SdMap? Map(int dungeonId) => _maps.TryGetValue(dungeonId, out var m) ? m : null;

    public async Task LoadAllAsync()
    {
        var story = new HashSet<int>();
        foreach (var e in await LoadAsync("dungeon"))
            if (e.TryGetProperty("id", out var id) && e.TryGetProperty("dungeonType", out var t) && t.GetString() == "STORY_DUNGEON")
                story.Add(id.GetInt32());
        var maps = new Dictionary<int, SdMap>();
        foreach (var e in await LoadAsync("dungeonMap"))
        {
            var map = e.Deserialize<SdMap>(JsonOptions);
            if (map is not null && story.Contains(map.id))
                maps[map.id] = map;
        }
        _maps = maps;
        logger.LogInformation("Story dungeon data: {Count} dungeons ({Ids})", _maps.Count, string.Join(", ", _maps.Keys.Order()));
    }

    private async Task<List<JsonElement>> LoadAsync(string dir)
    {
        var path = Path.Combine(staticDataRoot, dir);
        var result = new List<JsonElement>();
        if (!Directory.Exists(path))
        {
            logger.LogWarning("Story dungeon static data directory missing: {Path}", path);
            return result;
        }
        foreach (var file in Directory.EnumerateFiles(path, "*.json").OrderBy(f => f))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                using var doc = await JsonDocument.ParseAsync(stream);
                if (doc.RootElement.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                    result.AddRange(list.EnumerateArray().Select(x => x.Clone()));
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                logger.LogWarning(ex, "Skipping unreadable story dungeon file {File}", file);
            }
        }
        return result;
    }
}
