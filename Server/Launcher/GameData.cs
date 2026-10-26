using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace Server.Launcher;

/// <summary>
/// The game data the server needs, read from the player's own Limbus install, so the Windows
/// package carries none of Project Moon's files.
///
/// The tables (identities, items, Mirror Dungeon, banners...) ship as TextAssets in an addressable
/// bundle the game downloads into Unity's cache at its title screen; the English names lie loose
/// in the game folder. Both are copied next to the configured static-data path on first start,
/// and again whenever a newer bundle turns up after a game update.
/// </summary>
public static partial class GameData
{
    const string Marker = "/StaticData/static-data/personality/";

    /// <summary>Null when the data is ready, else why not, in words for the player.</summary>
    public static string? Prepare(Config config, out string? note)
    {
        note = null;
        var target = config.ResolvePath(config.StaticData.Path);          // .../game-data/static-data
        var root = Path.GetDirectoryName(target)!;                         // .../game-data
        var have = Directory.Exists(Path.Combine(target, "personality"));
        var stampPath = Path.Combine(root, "SOURCE");
        // root is replaced wholesale below: never let that be the server's own folder
        var dbDir = Path.GetDirectoryName(Path.GetFullPath(config.ResolvePath(config.Database.Path)));
        if (File.Exists(Path.Combine(root, "Config.json")) || string.Equals(dbDir, Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            return $"staticData.path must sit in a folder of its own (game-data/static-data), not {config.StaticData.Path}";
        try
        {
            var cache = CacheDir();
            if (cache is null || !Directory.Exists(cache))
                return have ? null : "Limbus hasn't been started on this PC yet.\n\nStart it once through Steam, "
                    + "wait for the title screen, close it, then open this app again.";
            var (bundle, tables) = FindBundle(cache);
            if (bundle is null)
                return have ? null : "Limbus hasn't downloaded its game data yet.\n\nStart it once through Steam, "
                    + "wait for the title screen, close it, then open this app again.";
            var info = new FileInfo(bundle);
            var stamp = $"{bundle}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
            if (have && File.Exists(stampPath) && File.ReadAllText(stampPath).Trim() == stamp)
                return null;

            var tmp = root + ".new";
            if (Directory.Exists(tmp))
                Directory.Delete(tmp, true);
            var n = 0;
            foreach (var (path, bytes) in tables!)
            {
                var at = path.IndexOf("/StaticData/static-data/", StringComparison.Ordinal);
                if (at < 0)
                    continue;
                var rel = path[(at + "/StaticData/".Length)..];
                if (rel.Split('/').Any(p => p is "" or "." or ".." || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                    continue;
                var dst = Path.Combine(tmp, Path.Combine(rel.Split('/')));
                Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                File.WriteAllBytes(dst, bytes);
                n++;
            }
            if (n < 500)
                throw new InvalidDataException($"only {n} tables in {bundle}");

            var names = 0;
            var game = GameDir();
            var loc = game is null ? null : Path.Combine(game, "LimbusCompany_Data", "Assets", "Resources_moved", "Localize", "en");
            if (loc is not null && Directory.Exists(loc))
            {
                Directory.CreateDirectory(Path.Combine(tmp, "names"));
                foreach (var f in Directory.EnumerateFiles(loc, "EN_*.json"))
                    if (NameFile().IsMatch(Path.GetFileName(f)))
                    {
                        File.Copy(f, Path.Combine(tmp, "names", Path.GetFileName(f)));
                        names++;
                    }
            }
            File.WriteAllText(Path.Combine(tmp, "SOURCE"), stamp + "\n");
            // swap, never delete first: a failed move must leave the old data where it was
            var old = root + ".old";
            if (Directory.Exists(old))
                Directory.Delete(old, true);
            if (Directory.Exists(root))
                Directory.Move(root, old);
            try
            {
                Directory.Move(tmp, root);
            }
            catch (IOException) when (Directory.Exists(old) && !Directory.Exists(root))
            {
                Directory.Move(old, root);
                throw;
            }
            try
            {
                if (Directory.Exists(old))
                    Directory.Delete(old, true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
            note = $"Game data read from this PC's Limbus: {n} tables, {names} name files ({bundle})";
            if (names == 0)
                note += " - Limbus's folder wasn't found, so the mod menu shows ids instead of names";
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException
                                       or IndexOutOfRangeException or ArgumentException)
        {
            if (have)
            {
                note = $"Kept the game data from before; reading the new one failed: {e.Message}";
                return null;
            }
            return $"Couldn't read Limbus's game data on this PC ({e.Message}).\n\nStart Limbus once through "
                + "Steam, wait for the title screen, close it, then open this app again.";
        }
    }

    [GeneratedRegex(@"^EN_(Personalities|Egos|Items)[^/\\]*\.json$")]
    private static partial Regex NameFile();

    // ---------------------------------------------------------------- where things are

    static string? CacheDir()
    {
        var env = Environment.GetEnvironmentVariable("MYOSOTIS_UNITY_CACHE");
        if (!string.IsNullOrEmpty(env))
            return env;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "AppData", "LocalLow", "Unity", "ProjectMoon_LimbusCompany");
    }

    static string? GameDir()
    {
        var env = Environment.GetEnvironmentVariable("MYOSOTIS_GAME_DIR");
        if (!string.IsNullOrEmpty(env))
            return env;
        var steams = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is string s)
                steams.Add(s.Replace('/', '\\'));
            steams.Add(@"C:\Program Files (x86)\Steam");
        }
        foreach (var steam in steams)
        {
            var libs = new List<string> { steam };
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
                libs.AddRange(LibraryPath().Matches(File.ReadAllText(vdf)).Select(m => m.Groups[1].Value.Replace(@"\\", @"\")));
            foreach (var lib in libs)
            {
                var dir = Path.Combine(lib, "steamapps", "common", "Limbus Company");
                if (Directory.Exists(Path.Combine(dir, "LimbusCompany_Data")))
                    return dir;
            }
        }
        return null;
    }

    [GeneratedRegex("\"path\"\\s+\"([^\"]+)\"")]
    private static partial Regex LibraryPath();

    /// <summary>The newest cached bundle holding the static tables, and its TextAssets.</summary>
    static (string? Path, List<(string Path, byte[] Bytes)>? Tables) FindBundle(string cache)
    {
        var marker = Encoding.ASCII.GetBytes(Marker);
        var candidates = Directory.EnumerateDirectories(cache)
            .SelectMany(d => Directory.EnumerateDirectories(d))
            .Select(d => new FileInfo(Path.Combine(d, "__data")))
            .Where(f => f.Exists && f.Length < 16 << 20)
            .OrderByDescending(f => f.LastWriteTimeUtc);
        foreach (var f in candidates)
        {
            try
            {
                var data = Unpack(File.ReadAllBytes(f.FullName));
                if (data.AsSpan().IndexOf(marker) < 0)
                    continue;
                var tables = TextAssets(data).ToList();
                if (tables.Count(t => t.Path.Contains(Marker, StringComparison.Ordinal)) > 0)
                    return (f.FullName, tables);
            }
            catch (Exception e) when (e is InvalidDataException or IndexOutOfRangeException or ArgumentException)
            {
            }
        }
        return (null, null);
    }

    // ---------------------------------------------------------------- UnityFS bundle

    /// <summary>A UnityFS bundle -> its first file (the serialized assets), unpacked.</summary>
    static byte[] Unpack(byte[] b)
    {
        var r = new Reader(b, bigEndian: true);
        if (r.CString() != "UnityFS")
            throw new InvalidDataException("not a UnityFS bundle");
        var format = r.U32();
        r.CString();                                   // player version
        r.CString();                                   // engine version (stripped)
        r.I64();                                       // total size
        var packedInfo = (int)r.U32();
        var infoSize = (int)r.U32();
        var flags = r.U32();
        if ((flags & 0x400) != 0)
            throw new InvalidDataException("encrypted bundle");
        if (format >= 7)
            r.Align(16);
        byte[] infoBytes;
        if ((flags & 0x80) != 0)                       // blocks info at the end
            infoBytes = b.AsSpan(b.Length - packedInfo, packedInfo).ToArray();
        else
            infoBytes = r.Bytes(packedInfo);
        var info = new Reader(Decompress(infoBytes, infoSize, (int)(flags & 0x3f)), bigEndian: true);
        info.Bytes(16);                                // hash
        var blocks = new (int Size, int Packed, int Flags)[info.I32()];
        for (var i = 0; i < blocks.Length; i++)
            blocks[i] = ((int)info.U32(), (int)info.U32(), info.U16());
        var nodes = new (long Offset, long Size)[info.I32()];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = (info.I64(), info.I64());
            info.U32();
            info.CString();
        }
        if ((flags & 0x200) != 0)
            r.Align(16);
        var all = new byte[blocks.Sum(x => (long)x.Size)];
        var at = 0;
        foreach (var (size, packed, bflags) in blocks)
        {
            Decompress(r.Bytes(packed), size, bflags & 0x3f).CopyTo(all, at);
            at += size;
        }
        if (nodes.Length == 0)
            throw new InvalidDataException("empty bundle");
        return all.AsSpan((int)nodes[0].Offset, (int)nodes[0].Size).ToArray();
    }

    static byte[] Decompress(byte[] src, int size, int kind) => kind switch
    {
        0 => src,
        2 or 3 => Lz4(src, size),
        _ => throw new InvalidDataException($"compression {kind} isn't supported"),
    };

    static byte[] Lz4(byte[] src, int size)
    {
        var dst = new byte[size];
        int s = 0, d = 0;
        while (s < src.Length)
        {
            var token = src[s++];
            var lit = token >> 4;
            if (lit == 15)
            {
                byte x;
                do lit += x = src[s++]; while (x == 255);
            }
            Array.Copy(src, s, dst, d, lit);
            s += lit;
            d += lit;
            if (s >= src.Length)
                break;
            var back = src[s] | src[s + 1] << 8;
            s += 2;
            var len = token & 15;
            if (len == 15)
            {
                byte x;
                do len += x = src[s++]; while (x == 255);
            }
            len += 4;
            if (back == 0 || back > d)
                throw new InvalidDataException("corrupt LZ4 block");
            for (var i = 0; i < len; i++, d++)
                dst[d] = dst[d - back];
        }
        if (d != size)
            throw new InvalidDataException("LZ4 block size mismatch");
        return dst;
    }

    // ---------------------------------------------------------------- serialized file

    /// <summary>(container path, bytes) for every TextAsset the bundle's AssetBundle lists.</summary>
    static IEnumerable<(string Path, byte[] Bytes)> TextAssets(byte[] f)
    {
        var r = new Reader(f, bigEndian: true);
        r.U32();
        r.U32();
        var version = r.U32();
        long dataOffset = r.U32();
        if (version < 22)
            throw new InvalidDataException($"serialized file version {version} isn't supported");
        var big = r.U8() != 0;
        r.Bytes(3);
        r.U32();                                       // metadata size
        r.I64();                                       // file size
        dataOffset = r.I64();
        r.I64();
        r.BigEndian = big;
        r.CString();                                   // unity version
        r.I32();                                       // target platform
        var typeTree = r.U8() != 0;
        var classes = new int[r.I32()];
        for (var i = 0; i < classes.Length; i++)
            classes[i] = SkipType(r, (int)version, typeTree, refType: false);
        var objects = new Dictionary<long, (long Start, int Size, int Class)>();
        var count = r.I32();
        for (var i = 0; i < count; i++)
        {
            r.Align(4);
            var id = r.I64();
            var start = r.I64() + dataOffset;
            var size = (int)r.U32();
            var type = r.I32();
            objects[id] = (start, size, classes[type]);
        }

        var bundle = objects.Values.FirstOrDefault(o => o.Class == 142);   // AssetBundle
        if (bundle.Size == 0)
            throw new InvalidDataException("no AssetBundle object");
        var a = new Reader(f, big) { Position = (int)bundle.Start };
        a.AlignedString();                             // m_Name
        var preload = a.I32();
        a.Position += preload * 12;                    // PPtr: int fileID + long pathID
        var entries = a.I32();
        for (var i = 0; i < entries; i++)
        {
            var path = a.AlignedString();
            a.I32();                                   // preloadIndex
            a.I32();                                   // preloadSize
            var file = a.I32();
            var id = a.I64();
            if (file != 0 || !objects.TryGetValue(id, out var o) || o.Class != 49)   // TextAsset
                continue;
            var t = new Reader(f, big) { Position = (int)o.Start };
            t.AlignedString();                         // m_Name
            yield return (path, t.Bytes(t.I32()));
        }
    }

    static int SkipType(Reader r, int version, bool typeTree, bool refType)
    {
        var cls = r.I32();
        r.U8();                                        // stripped
        var script = (short)r.U16();
        if ((refType && script >= 0) || cls == 114)
            r.Bytes(16);
        r.Bytes(16);
        if (!typeTree)
            return cls;
        var serialized = 1;
        if (version >= 23)
        {
            r.Bytes(16);
            serialized = r.I32();
        }
        if (serialized != 0)
        {
            var nodes = r.I32();
            var strings = r.I32();
            r.Position += nodes * (version >= 19 ? 32 : 24) + strings;
        }
        if (version >= 21)
        {
            if (refType)
            {
                r.CString();
                r.CString();
                r.CString();
            }
            else
            {
                var deps = r.I32();                    // read first: += would keep the old position
                r.Position += deps * 4;
            }
        }
        return cls;
    }

    sealed class Reader(byte[] b, bool bigEndian)
    {
        public int Position;
        public bool BigEndian = bigEndian;

        public byte U8() => b[Position++];
        public ushort U16() => Take(2, s => BigEndian ? BinaryPrimitives.ReadUInt16BigEndian(s) : BinaryPrimitives.ReadUInt16LittleEndian(s));
        public uint U32() => Take(4, s => BigEndian ? BinaryPrimitives.ReadUInt32BigEndian(s) : BinaryPrimitives.ReadUInt32LittleEndian(s));
        public int I32() => (int)U32();
        public long I64() => Take(8, s => BigEndian ? BinaryPrimitives.ReadInt64BigEndian(s) : BinaryPrimitives.ReadInt64LittleEndian(s));

        public byte[] Bytes(int n)
        {
            if (n < 0 || Position < 0 || Position + n > b.Length)
                throw new InvalidDataException("truncated");
            var x = b.AsSpan(Position, n).ToArray();
            Position += n;
            return x;
        }

        public string CString()
        {
            var end = Array.IndexOf(b, (byte)0, Position);
            if (end < 0)
                throw new InvalidDataException("truncated");
            var s = Encoding.UTF8.GetString(b, Position, end - Position);
            Position = end + 1;
            return s;
        }

        public string AlignedString()
        {
            var s = Encoding.UTF8.GetString(Bytes(I32()));
            Align(4);
            return s;
        }

        public void Align(int n) => Position = (Position + n - 1) / n * n;

        T Take<T>(int n, Func<ReadOnlySpan<byte>, T> read)
        {
            if (Position < 0 || Position + n > b.Length)
                throw new InvalidDataException("truncated");
            var v = read(b.AsSpan(Position, n));
            Position += n;
            return v;
        }
    }
}
