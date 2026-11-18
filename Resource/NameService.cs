using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Resource;

/// <summary>Display name for an identity or E.G.O. (e.g. sinner "Yi Sang", title "LCB Sinner").</summary>
public sealed record EntityName(int Id, string Sinner, string Title, int Rank);

/// <summary>
/// Loads English display names for identities and E.G.O.s so the tweaker can show
/// "Yi Sang - LCB Sinner" instead of 10101. Optional: if the localisation files are not
/// shipped, everything still works and the UI falls back to raw ids.
/// </summary>
public sealed class NameService(string staticDataRoot, ILogger<NameService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private Dictionary<int, EntityName> _personalities = [];
    private Dictionary<int, EntityName> _egos = [];
    private Dictionary<int, EntityName> _items = [];

    public IReadOnlyDictionary<int, EntityName> Personalities => _personalities;
    public IReadOnlyDictionary<int, EntityName> Egos => _egos;
    public IReadOnlyDictionary<int, EntityName> Items => _items;

    /// <summary>
    /// Display name for an item, e.g. "[Season 5] Don Quixote's Egoshard".
    ///
    /// Item localisation rows carry only <c>name</c>, which the shared loader puts in
    /// <see cref="EntityName.Sinner"/>; there is no <c>title</c> on them.
    /// </summary>
    public string ForItem(int id) =>
        _items.TryGetValue(id, out var n)
            ? (string.IsNullOrEmpty(n.Sinner) ? n.Title : n.Sinner)
            : "";

    public EntityName ForPersonality(int id) =>
        _personalities.TryGetValue(id, out var n) ? n : new EntityName(id, "", "", 0);

    public EntityName ForEgo(int id) =>
        _egos.TryGetValue(id, out var n) ? n : new EntityName(id, "", "", 0);

    /// <summary>Directories that might hold EN_Personalities*.json / EN_Egos*.json.</summary>
    private IEnumerable<string> LocalizeCandidates()
    {
        var sd = Path.GetFullPath(staticDataRoot);
        var up1 = Path.GetDirectoryName(sd);                       // .../StaticData
        var up2 = up1 is null ? null : Path.GetDirectoryName(up1);  // .../LimbusStaticData
        yield return Path.Combine(sd, "..", "names");
        if (up1 is not null) yield return Path.Combine(up1, "names");
        if (up2 is not null) yield return Path.Combine(up2, "Localize", "en");
        if (up2 is not null) yield return Path.Combine(up2, "names");
        if (up1 is not null) yield return Path.Combine(up1, "Localize", "en");
    }

    public async Task LoadAllAsync()
    {
        var dir = LocalizeCandidates()
            .Select(Path.GetFullPath)
            .FirstOrDefault(d => Directory.Exists(d) && Directory.EnumerateFiles(d, "EN_*.json").Any());

        if (dir is null)
        {
            logger.LogInformation("No localisation directory found; tweaker will show raw ids");
            return;
        }

        var ranks = await LoadRanksAsync();
        _personalities = await LoadNamesAsync(dir, "EN_Personalities*.json", ranks);
        _egos = await LoadNamesAsync(dir, "EN_Egos*.json", []);
        _items = await LoadNamesAsync(dir, "EN_Items*.json", []);

        logger.LogInformation("Names loaded from {Dir}: {P} identities, {E} egos, {I} items",
            dir, _personalities.Count, _egos.Count, _items.Count);
    }

    private async Task<Dictionary<int, int>> LoadRanksAsync()
    {
        var ranks = new Dictionary<int, int>();
        var dir = Path.Combine(staticDataRoot, "personality");
        if (!Directory.Exists(dir))
            return ranks;

        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                using var doc = await JsonDocument.ParseAsync(stream);
                if (!doc.RootElement.TryGetProperty("list", out var list))
                    continue;
                foreach (var e in list.EnumerateArray())
                {
                    if (!e.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id))
                        continue;
                    ranks[id] = e.TryGetProperty("rank", out var r) && r.TryGetInt32(out var rank) ? rank : 0;
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning("Skipping unreadable {File}: {Message}", file, ex.Message);
            }
        }
        return ranks;
    }

    private async Task<Dictionary<int, EntityName>> LoadNamesAsync(
        string dir, string pattern, Dictionary<int, int> ranks)
    {
        var result = new Dictionary<int, EntityName>();

        foreach (var file in Directory.EnumerateFiles(dir, pattern))
        {
            try
            {
                await using var stream = File.OpenRead(file);
                using var doc = await JsonDocument.ParseAsync(stream);
                if (!doc.RootElement.TryGetProperty("dataList", out var list))
                    continue;

                foreach (var e in list.EnumerateArray())
                {
                    if (!e.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id))
                        continue;

                    var sinner = e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var title = e.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    title = title.Replace('\n', ' ').Trim();
                    ranks.TryGetValue(id, out var rank);
                    result[id] = new EntityName(id, sinner, title, rank);
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning("Skipping unreadable {File}: {Message}", file, ex.Message);
            }
        }
        return result;
    }
}
