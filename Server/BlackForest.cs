using System.Text.Json;
using System.Text.Json.Nodes;
using Common;

namespace Server;

/// <summary>
/// The Black Forest: when Greencliff (a limbus-skins skin) dies in a fight on this machine, the
/// client patch reports it. That account's fight counts as won, and the gate to the
/// hidden page of the mod menu opens for it. Kept in blackforest.json beside Config.json, not
/// in the database, so nothing about the save format changes.
/// </summary>
public sealed class BlackForest
{
    static readonly object Gate = new();
    static BlackForest? _instance;

    readonly string _path;
    readonly HashSet<long> _open = [];
    readonly HashSet<long> _pendingWin = [];
    long _lastLocalBattler;

    BlackForest(string path)
    {
        _path = path;
        try
        {
            var doc = JsonSerializer.Deserialize<Saved>(File.ReadAllText(path));
            if (doc is not null)
            {
                _open.UnionWith(doc.Open ?? []);
                _pendingWin.UnionWith(doc.PendingWin ?? []);
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // first run, or a damaged file: everyone starts outside the forest
        }
        SyncUnlock();
    }

    public static BlackForest For(Config config)
    {
        lock (Gate)
            return _instance ??= new BlackForest(config.ResolvePath("blackforest.json"));
    }

    /// <summary>For tests: forget the loaded state so the next <see cref="For"/> reads the file again.</summary>
    public static void Reset()
    {
        lock (Gate)
            _instance = null;
    }

    /// <summary>A fight started. Only this machine's own client can fall in the forest.</summary>
    public void NoteBattle(long uid, bool local)
    {
        if (!local)
            return;
        lock (Gate)
            _lastLocalBattler = uid;
    }

    /// <summary>A fight ended: a fall after this (or in a fight that never went through
    /// EnterStageBattle, like Mirror Dungeon) must not be credited to it.</summary>
    public void EndBattle(long uid)
    {
        lock (Gate)
            if (_lastLocalBattler == uid)
                _lastLocalBattler = 0;
    }

    /// <summary>Greencliff fell. Returns the account it happened to, or 0 if no local fight is known.</summary>
    public long Fell()
    {
        lock (Gate)
        {
            if (_lastLocalBattler == 0)
                return 0;
            _open.Add(_lastLocalBattler);
            _pendingWin.Add(_lastLocalBattler);
            Save();
            SyncUnlock();
            return _lastLocalBattler;
        }
    }

    /// <summary>True once per fall: the fight that just ended counts as won.</summary>
    public bool TakeWin(long uid)
    {
        lock (Gate)
        {
            if (!_pendingWin.Remove(uid))
                return false;
            Save();
            return true;
        }
    }

    public bool IsOpen(long uid)
    {
        lock (Gate)
            return _open.Contains(uid);
    }

    public void Seal(long uid)
    {
        lock (Gate)
        {
            _open.Remove(uid);
            _pendingWin.Remove(uid);
            Save();
            SyncUnlock();
        }
    }

    // What the forest gives: limbus-skins keeps skins marked "unlock": "black-forest" out of
    // sight until its state lists that word. Open for any account: unlocked, and the rewards are
    // switched on the first time. Every gate sealed: locked, and they're switched off again.
    const string Unlock = "black-forest";
    static readonly string[] Rewards = ["kali"];

    static bool Holds(JsonArray a, string s) =>
        a.Any(x => x is JsonValue v && v.TryGetValue<string>(out var t) && t == s);

    static void Drop(JsonArray a, string s)
    {
        for (var i = a.Count - 1; i >= 0; i--)
            if (a[i] is JsonValue v && v.TryGetValue<string>(out var t) && t == s)
                a.RemoveAt(i);
    }

    void SyncUnlock()
    {
        var path = BlackForestScare.SkinsStatePath();
        if (!File.Exists(path))
            return;                                         // limbus-skins isn't set up on this machine
        try
        {
            if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                return;
            var unlocked = root["unlocked"] as JsonArray ?? [];
            var on = root["on"] as JsonArray ?? [];
            var want = _open.Count > 0;
            if (Holds(unlocked, Unlock) == want)
                return;
            if (want)
            {
                unlocked.Add(Unlock);
                foreach (var r in Rewards.Where(r => !Holds(on, r)))
                    on.Add(r);
            }
            else
            {
                Drop(unlocked, Unlock);
                foreach (var r in Rewards)
                    Drop(on, r);
            }
            root["unlocked"] = unlocked.Parent is null ? unlocked : unlocked.DeepClone();
            root["on"] = on.Parent is null ? on : on.DeepClone();
            var tmp = path + ".server.tmp";
            File.WriteAllText(tmp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException or InvalidOperationException)
        {
            // leave limbus-skins' state as it was rather than fail a battle request
        }
    }

    void Save()
    {
        // a failed write keeps the state in memory: it must never fail the battle request it rides on
        try
        {
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(new Saved([.. _open.Order()], [.. _pendingWin.Order()])));
            File.Move(tmp, _path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    sealed record Saved(List<long>? Open, List<long>? PendingWin);

    /// <summary>Whether a request came from this machine (the same test the mod menu uses).</summary>
    public static bool IsLocal(HttpContext context) => Tweak.LocalOnlyAttribute.IsLoopback(context);
}

/// <summary>
/// What a fall sets off on this machine. The client patch (myosotis BlackForestPatch) reports
/// Greencliff's death the moment BattleUnitModel_Player.OnDie runs for body 10701_*; this decides
/// whether it counts (limbus-skins has greencliff on) and plays the jumpscare: mpv with
/// art/blackforest-jumpscare.mp4 on Linux, Edge fullscreen on the WebM page on Windows.
///
/// Overrides, for tests: MYOSOTIS_SKINS_STATE (limbus-skins' state.json), MYOSOTIS_SCARE_CMD
/// (run with the clip page's URL instead of a player).
/// </summary>
public static class BlackForestScare
{
    /// <summary>limbus-skins' state.json (which skins are on, what is unlocked).</summary>
    public static string SkinsStatePath()
    {
        var state = Environment.GetEnvironmentVariable("MYOSOTIS_SKINS_STATE");
        return string.IsNullOrEmpty(state)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "limbus-skins", "state.json")
            : state;
    }

    public static bool Armed()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(SkinsStatePath()));
            return doc.RootElement.TryGetProperty("on", out var on) && on.ValueKind == JsonValueKind.Array
                && on.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString() == "greencliff");
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Plays the jumpscare. Null when it started, else why not.</summary>
    public static string? Play(Config config, string? clip)
    {
        var page = $"http://127.0.0.1:{config.Server.Port}/tweak/blackforest/scare";
        try
        {
            var test = Environment.GetEnvironmentVariable("MYOSOTIS_SCARE_CMD");
            if (!string.IsNullOrEmpty(test))
            {
                System.Diagnostics.Process.Start(test, page)?.Dispose();
                return null;
            }
            if (OperatingSystem.IsWindows())
                return Edge(page);
            if (clip is null)
                return "no blackforest-jumpscare.mp4 to play";
            // --no-config: a keep-open in the player's mpv.conf would hold the last frame over the game
            var psi = new System.Diagnostics.ProcessStartInfo("mpv") { UseShellExecute = false };
            foreach (var a in new[]
                     {
                         "--no-config", "--keep-open=no", "--fs", "--ontop", "--no-border", "--no-osc",
                         "--no-osd-bar", "--no-input-default-bindings", "--input-vo-keyboard=no", "--really-quiet",
                         "--title=The Black Forest", "--force-window=immediate", clip,
                     })
                psi.ArgumentList.Add(a);
            System.Diagnostics.Process.Start(psi)?.Dispose();
            return null;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return e.Message;
        }
    }

    /// <summary>Edge in kiosk mode on its own profile, closed once the clip has had time to end.</summary>
    static string? Edge(string page)
    {
        var edge = FindEdge();
        if (edge is null)
            return "Microsoft Edge wasn't found";
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var psi = new System.Diagnostics.ProcessStartInfo(edge) { UseShellExecute = false };
        foreach (var a in new[]
                 {
                     "--kiosk", page, "--edge-kiosk-type=fullscreen",
                     $"--user-data-dir={Path.Combine(local, "LimbusPrivateServer", "blackforest")}",
                     "--autoplay-policy=no-user-gesture-required", "--no-first-run", "--no-default-browser-check",
                     "--disable-sync", "--disable-background-networking", "--disable-breakpad",
                     "--disable-component-update", "--disable-features=msEdgeEnableSmartScreen",
                 })
            psi.ArgumentList.Add(a);
        var p = System.Diagnostics.Process.Start(psi);
        if (p is null)
            return "Edge didn't start";
        _ = Task.Delay(TimeSpan.FromSeconds(6)).ContinueWith(_ =>
        {
            try
            {
                if (!p.HasExited)
                    p.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            p.Dispose();
        });
        return null;
    }

    static string? FindEdge()
    {
        foreach (var v in new[] { "ProgramFiles(x86)", "ProgramFiles", "LOCALAPPDATA" })
        {
            var b = Environment.GetEnvironmentVariable(v);
            if (string.IsNullOrEmpty(b))
                continue;
            var p = Path.Combine(b, "Microsoft", "Edge", "Application", "msedge.exe");
            if (File.Exists(p))
                return p;
        }
        return null;
    }
}
