using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Common;

namespace Server.Launcher;

/// <summary>
/// The Windows app's updates, from this project's GitHub releases and nowhere else. GitHub is
/// asked only when the app's menu asks: when it opens (at most every 12 hours, and not at all
/// once that is switched off) or when the player presses Update. A download is used only if it
/// matches both the release's .sha256 file and GitHub's own digest of it; it is unpacked into
/// update\staged beside the app, which copies it over itself once the server has stopped (the server
/// can't replace its own files while it runs).
/// </summary>
public sealed partial class Updater(Config config, ILogger<Updater> logger)
{
    public static readonly TimeSpan Every = TimeSpan.FromHours(12);
    private const long MaxBytes = 400L << 20;
    private const string Package = "LimbusPrivateServer";

    private readonly Lock _gate = new();
    private Task? _download;

    /// <summary>idle, downloading, verifying, unpacking, ready (staged, waiting for the app) or failed.</summary>
    public string Phase { get; private set; } = "idle";
    public int Percent { get; private set; }
    public string Message { get; private set; } = "";

    private static string Api =>
        Environment.GetEnvironmentVariable("MYOSOTIS_UPDATE_API") is { Length: > 0 } a ? a.TrimEnd('/') : "https://api.github.com";

    private string StatePath => config.ResolvePath("update.json");
    /// <summary>update\ beside the app (the package's own folder), where the app looks for it.</summary>
    private string UpdateDir =>
        Path.Combine(Path.GetDirectoryName(config.ResolvePath(config.Launcher.AppPath)) ?? config.ResolvePath(".."), "update");
    public string StagedDir => Path.Combine(UpdateDir, "staged");
    /// <summary>A downloaded installer (the release's Setup.exe): the app hands the update to it.</summary>
    public string StagedSetup => Path.Combine(UpdateDir, "Setup.exe");

    /// <summary>The release this package is: 1.118.0, or 1.118.0-2 for a second build.</summary>
    public string Current => config.Launcher.Release;

    // ---------------------------------------------------------------- releases

    [GeneratedRegex(@"^(?:app-)?(\d+)\.(\d+)(?:\.(\d+))?(?:-(\d+))?$")]
    private static partial Regex ReleasePattern();

    /// <summary>(1, 118, 0, 2) from "app-1.118.0-2" or "1.118.0-2" (build 1 when there's no -N); null otherwise.</summary>
    public static (int, int, int, int)? Key(string? release)
    {
        var m = ReleasePattern().Match(release?.Trim() ?? "");
        if (!m.Success)
            return null;
        int Part(int i, int fallback) => m.Groups[i].Success ? int.Parse(m.Groups[i].Value) : fallback;
        return (Part(1, 0), Part(2, 0), Part(3, 0), Part(4, 1));
    }

    public static bool Newer(string? theirs, string? mine) =>
        Key(theirs) is { } t && Key(mine) is { } m && t.CompareTo(m) > 0;

    private static bool Allowed(string url) =>
        url.StartsWith("https://", StringComparison.Ordinal) || url.StartsWith("http://127.0.0.1:", StringComparison.Ordinal);

    private static HttpClient Client(TimeSpan timeout)
    {
        var http = new HttpClient { Timeout = timeout };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("LimbusPrivateServer");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return http;
    }

    public sealed record Release(string Tag, string Asset, string Url, long Size, string Digest, string SumsUrl);

    /// <summary>The newest release and its Windows package, from GitHub.</summary>
    private static async Task<Release> LatestAsync()
    {
        using var http = Client(TimeSpan.FromSeconds(10));
        var rel = JsonNode.Parse(await http.GetStringAsync($"{Api}/repos/paladiga484/myosotis-server/releases/latest"))
                  ?? throw new InvalidDataException("GitHub's answer was empty");
        var tag = rel["tag_name"]?.GetValue<string>() ?? "";
        if (Key(tag) is null)
            throw new InvalidDataException($"the newest release has an odd tag: {tag}");
        var version = Regex.Replace(tag.StartsWith("app-") ? tag[4..] : tag, @"-\d+$", "");
        JsonNode? Find(string name) =>
            (rel["assets"] as JsonArray ?? []).FirstOrDefault(a => a?["name"]?.GetValue<string>() == name);
        // the installer when the release has one (it updates in place, the Windows uninstall entry with
        // it); the package zip otherwise
        var asset = $"{Package}-Setup-{version}.exe";
        if (Find(asset) is null || Find(asset + ".sha256") is null)
            asset = $"{Package}-Windows-{version}.zip";
        var zip = Find(asset);
        var sums = Find(asset + ".sha256");
        if (zip is null || sums is null)
            throw new InvalidDataException($"release {tag} has no {asset} with its .sha256");
        return new Release(tag, asset, zip["browser_download_url"]?.GetValue<string>() ?? "",
            zip["size"]?.GetValue<long>() ?? 0, zip["digest"]?.GetValue<string>() ?? "",
            sums["browser_download_url"]?.GetValue<string>() ?? "");
    }

    // ---------------------------------------------------------------- what the menu sees

    private sealed class State
    {
        public bool Auto { get; set; } = true;
        public DateTime Checked { get; set; }
        public string Latest { get; set; } = "";
        public string Error { get; set; } = "";
        public Release? Info { get; set; }
    }

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private State Load()
    {
        try
        {
            return JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath), Json) ?? new State();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new State();
        }
    }

    private void Save(State st)
    {
        try
        {
            File.WriteAllText(StatePath + ".tmp", JsonSerializer.Serialize(st, Json));
            File.Move(StatePath + ".tmp", StatePath, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("update: couldn't keep the state: {Why}", e.Message);
        }
    }

    /// <summary>The state; GitHub is asked first when forced, or when it's due and the check is on.</summary>
    public async Task<object> CheckAsync(bool force)
    {
        var st = Load();
        var due = st.Auto && DateTime.UtcNow - st.Checked >= Every;
        if (Key(Current) is not null && (force || due))
        {
            try
            {
                var info = await LatestAsync();
                (st.Checked, st.Latest, st.Info, st.Error) = (DateTime.UtcNow, info.Tag, info, "");
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidDataException
                                          or JsonException or InvalidOperationException or FormatException)
            {
                st.Checked = DateTime.UtcNow - Every + TimeSpan.FromHours(1);   // offline: try again in an hour
                st.Error = e is TaskCanceledException ? "GitHub didn't answer in time" : e.Message;
            }
            Save(st);
        }
        return View(st);
    }

    public object Status() => View(Load());

    private object View(State st)
    {
        lock (_gate)
            return new
            {
                current = Current,
                latest = st.Latest,
                newer = Key(Current) is not null && Newer(st.Latest, Current) && st.Info is not null,
                auto = st.Auto,
                @checked = st.Checked == default ? "" : st.Checked.ToString("yyyy-MM-dd HH:mm") + " UTC",
                error = st.Error,
                phase = Phase,
                percent = Percent,
                message = Message,
            };
    }

    public void SetAuto(bool on)
    {
        var st = Load();
        st.Auto = on;
        Save(st);
    }

    // ---------------------------------------------------------------- downloading

    /// <summary>Start fetching the newest release into update\staged. Null when under way, else why not.</summary>
    public string? Start()
    {
        var st = Load();
        if (Key(Current) is null)
            return "This copy doesn't know its own release, so it can't update itself.";
        if (st.Info is null || !Newer(st.Latest, Current))
            return "There's nothing newer to install.";
        if (LauncherService.GameRunning())
            return "Close Limbus first: the update replaces the server it plays on.";
        lock (_gate)
        {
            if (_download is { IsCompleted: false })
                return "The update is already downloading.";
            (Phase, Percent, Message) = ("downloading", 0, $"Downloading {st.Latest}");
            _download = Task.Run(() => FetchAsync(st.Info));
        }
        return null;
    }

    private void Set(string phase, string message, int percent = 0)
    {
        lock (_gate)
            (Phase, Message, Percent) = (phase, message, percent);
    }

    private async Task FetchAsync(Release rel)
    {
        var zip = Path.Combine(UpdateDir, rel.Asset.EndsWith(".exe", StringComparison.Ordinal) ? "download.exe" : "download.zip");
        try
        {
            if (!Allowed(rel.Url) || !Allowed(rel.SumsUrl))
                throw new InvalidDataException("refusing a download that isn't https");
            Directory.CreateDirectory(UpdateDir);
            using var http = Client(TimeSpan.FromMinutes(15));

            var sums = (await http.GetStringAsync(rel.SumsUrl)).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var want = sums.Length > 0 ? sums[0].ToLowerInvariant() : "";
            if (!Regex.IsMatch(want, "^[0-9a-f]{64}$") || (sums.Length > 1 && sums[1].TrimStart('*') != rel.Asset))
                throw new InvalidDataException("the release's .sha256 file isn't right");
            if (rel.Digest.Length > 0 && !rel.Digest.Equals("sha256:" + want, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("GitHub's checksum and the release's .sha256 file disagree");

            using (var response = await http.GetAsync(rel.Url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var body = await response.Content.ReadAsStreamAsync();
                await using var file = File.Create(zip);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[1 << 16];
                long got = 0;
                int n;
                while ((n = await body.ReadAsync(buffer)) > 0)
                {
                    got += n;
                    if (got > MaxBytes)
                        throw new InvalidDataException("the download is far bigger than a release");
                    hash.AppendData(buffer, 0, n);
                    await file.WriteAsync(buffer.AsMemory(0, n));
                    if (rel.Size > 0)
                        Set("downloading", $"Downloading {rel.Tag}", (int)Math.Min(100, got * 100 / rel.Size));
                }
                Set("verifying", "Checking the download");
                if (Convert.ToHexStringLower(hash.GetHashAndReset()) != want)
                    throw new InvalidDataException("the download doesn't match its checksum");
            }

            if (rel.Asset.EndsWith(".exe", StringComparison.Ordinal))
            {
                if (Directory.Exists(StagedDir))
                    Directory.Delete(StagedDir, true);
                File.Move(zip, StagedSetup, true);
                File.WriteAllText(Path.Combine(UpdateDir, "staged.txt"), rel.Tag);
                Set("ready", $"{rel.Tag} is ready: the app restarts into it", 100);
                logger.LogInformation("update: {Tag}'s installer downloaded and checked", rel.Tag);
                return;
            }
            Set("unpacking", "Unpacking");
            if (Directory.Exists(StagedDir))
                Directory.Delete(StagedDir, true);
            ZipFile.ExtractToDirectory(zip, StagedDir);          // refuses entries that point outside it
            var top = Path.Combine(StagedDir, Package);
            if (!File.Exists(Path.Combine(top, "Limbus Private Server.exe")) || !File.Exists(Path.Combine(top, "server", "Server.exe")))
                throw new InvalidDataException("the download isn't a Windows package");
            File.WriteAllText(Path.Combine(UpdateDir, "staged.txt"), rel.Tag);
            Set("ready", $"{rel.Tag} is ready: the app restarts into it", 100);
            logger.LogInformation("update: {Tag} downloaded, checked and unpacked", rel.Tag);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidDataException
                                      or IOException or UnauthorizedAccessException)
        {
            try { if (Directory.Exists(StagedDir)) Directory.Delete(StagedDir, true); } catch (IOException) { }
            try { File.Delete(StagedSetup); } catch (IOException) { }
            Set("failed", (e is TaskCanceledException ? "the download timed out" : e.Message) + " - nothing was changed");
            logger.LogWarning("update: {Why}", e.Message);
        }
        finally
        {
            try { File.Delete(zip); } catch (IOException) { }
        }
    }
}
