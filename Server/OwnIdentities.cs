using System.Text.Json;
using Types.Server;

namespace Server;

/// <summary>
/// Identities of our own (limbus-skins "identity" skins: Vergilius as 10799, Kali as 10499): new
/// ids cloned into the client's static data. Only a client that has them may see them, so they
/// are granted only while all of this holds:
///   - a limbus-skins session is applied on this machine (its active.json lists the shared
///     static-data file, which carries the clones) and its helpers manifest lists them;
///   - the request comes from this machine (a friend's client over Tailscale has stock files);
///   - for Kali, the account's Black Forest is open.
/// Otherwise every id the server's own game data doesn't know is kept out of what the client
/// sees - failing closed, with no memory to lose - and a formation slot or battle result holding
/// one shows the sinner's default identity. Nothing is ever deleted from the save.
///
/// Override for tests: MYOSOTIS_SKINS_DATA (limbus-skins' data folder).
/// </summary>
public static class OwnIdentities
{
    public sealed record Own(int Id, int Base, string? Unlock);

    static string DataDir()
    {
        var data = Environment.GetEnvironmentVariable("MYOSOTIS_SKINS_DATA");
        return string.IsNullOrEmpty(data)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "limbus-skins")
            : data;
    }

    /// <summary>A limbus-skins session has the shared static-data file swapped in right now.</summary>
    static bool SessionApplied()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir(), "active.json")));
            return doc.RootElement.TryGetProperty("mods", out var mods) && mods.ValueKind == JsonValueKind.Array
                && mods.EnumerateArray().Any(m => m.ValueKind == JsonValueKind.String && m.GetString() == "helpers");
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return false;                                  // no session: stock files
        }
    }

    static List<Own> Built()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(DataDir(), "built", "helpers", "manifest.json")));
            if (!doc.RootElement.TryGetProperty("identities", out var list) || list.ValueKind != JsonValueKind.Array)
                return [];
            var out_ = new List<Own>();
            foreach (var e in list.EnumerateArray())
                if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var id) && id.TryGetInt32(out var i)
                    && e.TryGetProperty("base", out var b) && b.TryGetInt32(out var bi))
                    out_.Add(new Own(i, bi, e.TryGetProperty("unlock", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null));
            return out_;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException)
        {
            return [];
        }
    }

    /// <summary>The ids this request's client has right now (empty for another machine's).</summary>
    public static HashSet<int> Active(Common.Config config, long uid, bool local)
    {
        if (!local || !SessionApplied())
            return [];
        return Built()
            .Where(o => o.Unlock is null || (o.Unlock == "black-forest" && BlackForest.For(config).IsOpen(uid)))
            .Select(o => o.Id)
            .ToHashSet();
    }

    /// <summary>The id the client may be shown in place of `id`: itself if the game data knows it
    /// or it's active, else the sinner's default identity (1SS01).</summary>
    public static int Shown(int id, IReadOnlySet<int> active, IReadOnlySet<int> known) =>
        known.Contains(id) || active.Contains(id) ? id : id / 100 * 100 + 1;

    public static List<PersonalityFormat> Visible(List<PersonalityFormat> all, IReadOnlySet<int> active, IReadOnlySet<int> known) =>
        all.Where(p => known.Contains(p.personality_id) || active.Contains(p.personality_id)).ToList();

    /// <summary>Formations as the client may see them; the stored ones are left as they are.</summary>
    public static List<FormationFormat> Formations(List<FormationFormat> all, IReadOnlySet<int> active, IReadOnlySet<int> known) =>
        all.Select(f => f.formationDetails.All(d => Shown(d.personalityId, active, known) == d.personalityId) ? f : new FormationFormat
        {
            id = f.id,
            formationNameFormat = f.formationNameFormat,
            formationDetails = f.formationDetails.Select(d => new FormationDetailFormat
            {
                personalityId = Shown(d.personalityId, active, known),
                egos = d.egos,
                isParticipated = d.isParticipated,
                participationOrder = d.participationOrder,
                skinId = d.skinId,
            }).ToList(),
        }).ToList();
}
