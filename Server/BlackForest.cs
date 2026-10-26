using System.Text;
using System.Text.Json;
using Common;

namespace Server;

/// <summary>
/// The Black Forest: when Greencliff (a limbus-skins skin) dies in a fight on this machine, the
/// watcher beside the game reports it. That account's fight counts as won, and the gate to the
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
/// The Windows app's half of the Black Forest (on Linux, blackforest.py beside patchlc does it).
/// Follows Limbus's Player.log; "cannot find idbattle_dead_10701_1" is the moment LCB Heathcliff
/// dies. While limbus-skins has greencliff on, that opens the gate and plays the jumpscare in an
/// Edge window over the game.
///
/// Overrides, for tests: MYOSOTIS_PLAYER_LOG (the log; also starts the watcher off Windows),
/// MYOSOTIS_SKINS_STATE (limbus-skins' state.json), MYOSOTIS_SCARE_CMD (run instead of Edge).
/// </summary>
public static partial class BlackForestWatcher
{
    [System.Text.RegularExpressions.GeneratedRegex(@"cannot find idbattle_dead_10701_\d")]
    private static partial System.Text.RegularExpressions.Regex Death();

    const int QuietSeconds = 30;

    public static void Start(Config config, ILogger log)
    {
        var path = Environment.GetEnvironmentVariable("MYOSOTIS_PLAYER_LOG");
        if (string.IsNullOrEmpty(path))
        {
            if (!(OperatingSystem.IsWindows() && config.Launcher.Enabled))
                return;
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, "AppData", "LocalLow", "ProjectMoon", "LimbusCompany", "Player.log");
        }
        var thread = new Thread(() => Run(config, log, path)) { IsBackground = true, Name = "black forest" };
        thread.Start();
        log.LogInformation("The Black Forest watches {Log}", path);
    }

    static void Run(Config config, ILogger log, string path)
    {
        var last = DateTime.MinValue;
        foreach (var line in Follow(path))
        {
            if (!Death().IsMatch(line) || !Armed())
                continue;
            if ((DateTime.UtcNow - last).TotalSeconds < QuietSeconds)
                continue;
            last = DateTime.UtcNow;
            var uid = BlackForest.For(config).Fell();
            log.LogInformation("Greencliff fell; the Black Forest opened for uid {Uid}", uid);
            try
            {
                Scare(config);
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
            {
                log.LogWarning("The jumpscare couldn't play: {Error}", e.Message);
            }
        }
    }

    /// <summary>New lines of a log Unity rewrites on every start: from its end the first time,
    /// from the top of each new one. Never ends.</summary>
    static IEnumerable<string> Follow(string path)
    {
        FileStream? fs = null;
        StreamReader? reader = null;
        long pos = 0;
        var created = DateTime.MinValue;
        var first = true;
        var pending = new StringBuilder();
        while (true)
        {
            var info = new FileInfo(path);
            // a new game session rewrites the log: it shrinks, and on Windows it is a new file
            // (elsewhere the creation time can follow the last write, so it can't tell)
            if (info.Exists && (fs is null || info.Length < pos
                                || (OperatingSystem.IsWindows() && info.CreationTimeUtc != created)))
            {
                reader?.Dispose();
                fs?.Dispose();
                try
                {
                    fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }
                catch (IOException)
                {
                    fs = null;
                    Thread.Sleep(250);
                    continue;
                }
                created = info.CreationTimeUtc;
                pos = first ? fs.Length : 0;
                fs.Position = pos;
                reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
                pending.Clear();
            }
            first = false;
            string? chunk = reader is null ? null : reader.ReadToEnd();
            if (!string.IsNullOrEmpty(chunk))
            {
                pos = fs!.Position;
                pending.Append(chunk);
                var text = pending.ToString();
                var cut = text.LastIndexOf('\n');
                if (cut >= 0)
                {
                    pending.Clear().Append(text[(cut + 1)..]);       // a half-written line waits
                    foreach (var line in text[..cut].Split('\n'))
                        yield return line;
                }
            }
            Thread.Sleep(250);
        }
    }

    static bool Armed()
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

    /// <summary>Edge, fullscreen and without chrome, on the page that plays the clip; closed once
    /// the clip has had time to end.</summary>
    static void Scare(Config config)
    {
        var url = $"http://127.0.0.1:{config.Server.Port}/tweak/blackforest/scare";
        var test = Environment.GetEnvironmentVariable("MYOSOTIS_SCARE_CMD");
        if (!string.IsNullOrEmpty(test))
        {
            System.Diagnostics.Process.Start(test, url)?.Dispose();
            return;
        }
        var edge = FindEdge() ?? throw new InvalidOperationException("Microsoft Edge wasn't found");
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var psi = new System.Diagnostics.ProcessStartInfo(edge) { UseShellExecute = false };
        foreach (var a in new[]
                 {
                     "--kiosk", url, "--edge-kiosk-type=fullscreen",
                     $"--user-data-dir={Path.Combine(local, "LimbusPrivateServer", "blackforest")}",
                     "--autoplay-policy=no-user-gesture-required", "--no-first-run", "--no-default-browser-check",
                     "--disable-sync", "--disable-background-networking", "--disable-breakpad",
                     "--disable-component-update", "--disable-features=msEdgeEnableSmartScreen",
                 })
            psi.ArgumentList.Add(a);
        var p = System.Diagnostics.Process.Start(psi);
        if (p is null)
            return;
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
