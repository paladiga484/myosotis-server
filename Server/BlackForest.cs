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
