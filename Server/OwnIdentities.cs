using System.Text.Json;
using Common;
using Types.Server;

namespace Server;

/// <summary>
/// Identities of our own (limbus-skins "identity" skins: Vergilius as 10799, Kali as 10499): new
/// ids cloned into the client's static data. They exist only while limbus-skins has them built
/// (its helpers manifest lists them), so the server grants them then - Kali only to an account
/// whose Black Forest is open - and otherwise keeps them out of what the client sees: a client
/// without them in its data would trip over an id it doesn't know. Nothing is deleted; every id
/// ever seen is remembered in own-identities.json with its base, so a formation slot holding a
/// hidden one shows the identity it was cloned from instead.
///
/// Override for tests: MYOSOTIS_SKINS_DATA (limbus-skins' data folder).
/// </summary>
public sealed class OwnIdentities
{
    public sealed record Own(int Id, int Base, string? Unlock);

    static readonly object Gate = new();
    static OwnIdentities? _instance;

    readonly Config _config;
    readonly string _knownPath;
    readonly Dictionary<int, int> _known = [];

    OwnIdentities(Config config)
    {
        _config = config;
        _knownPath = config.ResolvePath("own-identities.json");
        try
        {
            var saved = JsonSerializer.Deserialize<Dictionary<int, int>>(File.ReadAllText(_knownPath));
            if (saved is not null)
                foreach (var (id, b) in saved)
                    _known[id] = b;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // first run: none seen yet
        }
    }

    public static OwnIdentities For(Config config)
    {
        lock (Gate)
            return _instance ??= new OwnIdentities(config);
    }

    /// <summary>For tests: forget the loaded state.</summary>
    public static void Reset()
    {
        lock (Gate)
            _instance = null;
    }

    static string ManifestPath()
    {
        var data = Environment.GetEnvironmentVariable("MYOSOTIS_SKINS_DATA");
        if (string.IsNullOrEmpty(data))
            data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "limbus-skins");
        return Path.Combine(data, "built", "helpers", "manifest.json");
    }

    static List<Own> Built()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ManifestPath()));
            if (!doc.RootElement.TryGetProperty("identities", out var list) || list.ValueKind != JsonValueKind.Array)
                return [];
            var out_ = new List<Own>();
            foreach (var e in list.EnumerateArray())
                if (e.TryGetProperty("id", out var id) && id.TryGetInt32(out var i)
                    && e.TryGetProperty("base", out var b) && b.TryGetInt32(out var bi))
                    out_.Add(new Own(i, bi, e.TryGetProperty("unlock", out var u) ? u.GetString() : null));
            return out_;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException)
        {
            return [];                                     // limbus-skins not set up here, or nothing built
        }
    }

    /// <summary>The ones this account has right now (and remembers every id seen).</summary>
    public List<Own> Active(long uid)
    {
        var built = Built();
        lock (Gate)
        {
            var grew = false;
            foreach (var o in built)
                if (!_known.TryGetValue(o.Id, out var b) || b != o.Base)
                {
                    _known[o.Id] = o.Base;
                    grew = true;
                }
            if (grew)
                Save();
        }
        return built.Where(o => o.Unlock is null || (o.Unlock == "black-forest" && BlackForest.For(_config).IsOpen(uid))).ToList();
    }

    /// <summary>The personalities the client may see: none of ours that isn't active.</summary>
    public List<PersonalityFormat> Visible(List<PersonalityFormat> all, IReadOnlyCollection<Own> active)
    {
        var shown = active.Select(o => o.Id).ToHashSet();
        lock (Gate)
            return all.Where(p => !_known.ContainsKey(p.personality_id) || shown.Contains(p.personality_id)).ToList();
    }

    /// <summary>Formations as the client may see them: a slot holding one of ours that isn't active
    /// shows its base. The stored formation is left as it is (copies are made where needed).</summary>
    public List<FormationFormat> Formations(List<FormationFormat> all, IReadOnlyCollection<Own> active)
    {
        var shown = active.Select(o => o.Id).ToHashSet();
        Dictionary<int, int> known;
        lock (Gate)
            known = new Dictionary<int, int>(_known);
        bool Hidden(int id) => known.ContainsKey(id) && !shown.Contains(id);
        return all.Select(f => !f.formationDetails.Any(d => Hidden(d.personalityId)) ? f : new FormationFormat
        {
            id = f.id,
            formationNameFormat = f.formationNameFormat,
            formationDetails = f.formationDetails.Select(d => !Hidden(d.personalityId) ? d : new FormationDetailFormat
            {
                personalityId = known[d.personalityId],
                egos = d.egos,
                isParticipated = d.isParticipated,
                participationOrder = d.participationOrder,
                skinId = d.skinId,
            }).ToList(),
        }).ToList();
    }

    void Save()
    {
        // a failed write keeps the map in memory: never fail the login it rides on
        try
        {
            var tmp = _knownPath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_known.OrderBy(kv => kv.Key).ToDictionary()));
            File.Move(tmp, _knownPath, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }
}
