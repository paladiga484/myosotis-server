using System.Text.Json;
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
        }
    }

    void Save()
    {
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(new Saved([.. _open.Order()], [.. _pendingWin.Order()])));
        File.Move(tmp, _path, overwrite: true);
    }

    sealed record Saved(List<long>? Open, List<long>? PendingWin);

    /// <summary>Whether a request came from this machine (the same test the mod menu uses).</summary>
    public static bool IsLocal(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is { IsIPv4MappedToIPv6: true })
            remote = remote.MapToIPv4();
        return remote is not null && System.Net.IPAddress.IsLoopback(remote);
    }
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
    public static bool Armed()
    {
        var state = Environment.GetEnvironmentVariable("MYOSOTIS_SKINS_STATE");
        if (string.IsNullOrEmpty(state))
            state = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "limbus-skins", "state.json");
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(state));
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
