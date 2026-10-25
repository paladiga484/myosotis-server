using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Common;

namespace Server.Launcher;

/// <summary>
/// What the Windows app's window drives: starting Limbus through Steam and attaching the patch
/// (myoink.exe, out of sight), the settings for a friend's server, hosting for friends, and
/// stopping the server once the window is gone and nothing needs it.
/// </summary>
public sealed partial class LauncherService(
    Config config,
    IHostApplicationLifetime lifetime,
    ILogger<LauncherService> logger) : BackgroundService
{
    public const string SteamUri = "steam://rungameid/1973530";
    private const string HostPlaceholder = "HOST-ADDRESS";
    private const string TokenPlaceholder = "CHANGE-ME";

    private readonly Lock _gate = new();
    private Process? _inject;
    private DateTime _lastSeen = DateTime.UtcNow.AddSeconds(60);   // the window gets a minute to open

    public string Mode { get; private set; } = "";
    public string Phase { get; private set; } = "idle";             // idle launching waiting attaching attached failed
    public string Message { get; private set; } = "";

    /// <summary>The window is open: it asks for the state every second.</summary>
    public void Seen() => _lastSeen = DateTime.UtcNow;

    public static bool GameRunning() => Process.GetProcessesByName("LimbusCompany").Length > 0;

    public bool Busy
    {
        get { lock (_gate) return _inject is { HasExited: false }; }
    }

    /// <summary>This machine's address friends connect to, when hosting.</summary>
    public string? HostingAddress =>
        config.Server.Hosts().FirstOrDefault(h => IPAddress.TryParse(h.Trim('[', ']'), out var a) && !IPAddress.IsLoopback(a));

    // ---------------------------------------------------------------- playing

    /// <summary>Start Limbus (unless it is up) and attach the patch. Null when under way, else why not.</summary>
    public string? Play(string mode)
    {
        if (mode is not ("local" or "join"))
            return "Unknown way to play.";
        if (!OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("MYOSOTIS_NO_STEAM") != "1")
            return "Playing from this window is for the Windows package.";
        var dir = config.ResolvePath(mode == "join" ? config.Launcher.JoinDir : config.Launcher.ClientDir);
        var exe = Path.Combine(dir, "myoink.exe");
        if (!File.Exists(exe) || !File.Exists(Path.Combine(dir, "myosotis.dll")))
            return $"The patch is missing from {dir} - unzip the whole folder again.";
        if (mode == "join")
        {
            var (host, tokenSet) = ReadJoin();
            if (host.Length == 0 || !tokenSet)
                return "Fill in your friend's address and your token first.";
        }

        lock (_gate)
        {
            if (_inject is { HasExited: false })
                return null;                                     // already on its way
            Mode = mode;
            Phase = "launching";
            Message = "Starting Limbus through Steam";
            if (!GameRunning() && Environment.GetEnvironmentVariable("MYOSOTIS_NO_STEAM") != "1")
            {
                try
                {
                    Process.Start(new ProcessStartInfo(SteamUri) { UseShellExecute = true });
                }
                catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    Phase = "failed";
                    Message = "Steam didn't open. Is it installed, and Limbus in your library?";
                    return Message;
                }
            }

            var psi = new ProcessStartInfo(exe)
            {
                WorkingDirectory = dir,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            var p = new Process { StartInfo = psi };
            p.ErrorDataReceived += (_, e) => OnLine(e.Data);
            p.OutputDataReceived += (_, e) => OnLine(e.Data);
            try
            {
                p.Start();
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                Phase = "failed";
                Message = $"The patch tool wouldn't start ({e.Message}). Antivirus can block it - see README.txt.";
                return Message;
            }
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            _inject = p;
            _ = Task.Run(() =>
            {
                p.WaitForExit();                                 // also waits for the last output lines
                OnExit(p, p.ExitCode);
            });
        }
        logger.LogInformation("Launcher: play ({Mode})", mode);
        return null;
    }

    public void Cancel()
    {
        lock (_gate)
        {
            if (_inject is { HasExited: false } p)
            {
                try { p.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            }
            _inject = null;
            Phase = "idle";
            Message = "";
        }
    }

    private void OnLine(string? line)
    {
        if (line is null)
            return;
        lock (_gate)
        {
            if (line.Contains("waiting for window"))
                (Phase, Message) = ("waiting", "Waiting for Limbus to open");
            else if (line.Contains("found window"))
                (Phase, Message) = ("attaching", "Limbus is open - attaching the patch");
            else if (line.Contains("injected"))
                (Phase, Message) = ("attached", Arrived());
        }
    }

    private void OnExit(Process p, int code)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(p, _inject))
                return;                                          // cancelled, or replaced by a newer run
            _inject = null;
            if (code == 0)
            {
                (Phase, Message) = ("attached", Arrived());
                return;
            }
            Phase = "failed";
            Message = code switch
            {
                2 => "myosotis.dll is missing next to myoink.exe - unzip the whole folder again.",
                20 => "Limbus didn't open within 5 minutes. Press Play again once Steam has it running.",
                22 => "Windows wouldn't let the patch in. Let Limbus reach the title screen, then press Play again.",
                23 => "The patch didn't attach. Let Limbus reach the title screen, then press Play again.",
                _ => $"The patch tool stopped unexpectedly (code {code}). Press Play to try again.",
            };
        }
        logger.LogInformation("Launcher: patch tool exited {Code}", code);
    }

    private string Arrived() =>
        Mode == "join" ? "You're on your friend's server" : "You're on your own server";

    /// <summary>The phase as the window should show it: an attached run ends when Limbus closes.</summary>
    public (string Mode, string Phase, string Message) Now()
    {
        lock (_gate)
        {
            if (Phase == "attached" && !GameRunning())
                (Phase, Message) = ("idle", "");
            return (Mode, Phase, Message);
        }
    }

    // ---------------------------------------------------------------- a friend's server

    private string JoinIni => Path.Combine(config.ResolvePath(config.Launcher.JoinDir), "myosotis.ini");

    /// <summary>(friend's address, whether a token of your own is set) from join\myosotis.ini.</summary>
    public (string Host, bool TokenSet) ReadJoin()
    {
        if (!File.Exists(JoinIni))
            return ("", false);
        string host = "", token = "";
        foreach (var raw in File.ReadAllLines(JoinIni))
        {
            var line = raw.Trim();
            if (line.StartsWith("token=", StringComparison.OrdinalIgnoreCase))
                token = line[6..].Trim();
            else if (line.StartsWith("server=", StringComparison.OrdinalIgnoreCase))
                host = HostOfUrl(line[7..].Trim());
        }
        if (host.Contains(HostPlaceholder, StringComparison.OrdinalIgnoreCase))
            host = "";
        return (host, token.Length > 0 && !token.Contains(TokenPlaceholder, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Save the friend's address, and a token unless left empty (keep the old one).</summary>
    public string? WriteJoin(string host, string? token)
    {
        host = host.Trim();
        token = token?.Trim() ?? "";
        if (!ValidHost(host))
            return "That doesn't look like an address. Use the one your friend sees in Tailscale, like 100.64.0.7.";
        if (token.Length > 0 && !TokenPattern().IsMatch(token))
            return "Tokens are 12 to 128 letters, digits, dots, dashes or underscores.";
        if (token.Length == 0 && !ReadJoin().TokenSet)
            return "Set a token of your own too - it is your account on their server.";
        if (!File.Exists(JoinIni))
            return $"{JoinIni} is missing - unzip the whole folder again.";

        var url = $"http://{UrlHost(host)}:3000/";
        var lines = File.ReadAllLines(JoinIni).ToList();
        void Set(string key, string value)
        {
            var i = lines.FindIndex(l => l.TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            if (i >= 0) lines[i] = $"{key}={value}";
            else lines.Add($"{key}={value}");
        }
        Set("server", url);
        Set("serverinfos_url", url + "serverinfos.json");
        if (token.Length > 0)
            Set("token", token);
        var tmp = JoinIni + ".tmp";
        File.WriteAllText(tmp, string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(false));
        File.Move(tmp, JoinIni, overwrite: true);
        logger.LogInformation("Launcher: friend's server set to {Host}", host);
        return null;
    }

    /// <summary>Ask the friend's server which game versions it takes. (reachable, versions or why not)</summary>
    public async Task<(bool Ok, string[] Versions, string Error)> TestAsync(string host)
    {
        host = host.Trim();
        if (!ValidHost(host))
            return (false, [], "That doesn't look like an address.");
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        try
        {
            var body = await http.GetStringAsync($"http://{UrlHost(host)}:3000/serverinfos.json");
            var versions = (JsonNode.Parse(body) as JsonArray)?
                .SelectMany(e => (e?["versions"] as JsonArray ?? []).Select(v => v?.GetValue<string>() ?? ""))
                .Where(v => v.Length > 0).Distinct().ToArray() ?? [];
            return versions.Length > 0
                ? (true, versions, "")
                : (false, [], "Something answered, but it isn't a Limbus private server.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return (false, [], "No answer. Is Tailscale on for both of you, and their server running?");
        }
    }

    public static bool ValidHost(string host) =>
        host.Length is > 0 and <= 253
        && host != HostPlaceholder
        && (IPAddress.TryParse(host.Trim('[', ']'), out _) || HostnamePattern().IsMatch(host));

    private static string UrlHost(string host)
    {
        var bare = host.Trim('[', ']');
        return IPAddress.TryParse(bare, out var a) && a.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{bare}]" : host;
    }

    private static string HostOfUrl(string url)
    {
        var s = url;
        var scheme = s.IndexOf("://", StringComparison.Ordinal);
        if (scheme >= 0)
            s = s[(scheme + 3)..];
        s = s.TrimEnd('/');
        if (s.StartsWith('['))
            return s[1..Math.Max(1, s.IndexOf(']'))];
        var colon = s.LastIndexOf(':');
        return colon > 0 ? s[..colon] : s;
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{12,128}$")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"^(?=.{1,253}$)([A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)*[A-Za-z0-9]([A-Za-z0-9-]{0,61}[A-Za-z0-9])?$")]
    private static partial Regex HostnamePattern();

    // ---------------------------------------------------------------- hosting for friends

    /// <summary>This machine's Tailscale addresses (100.64.0.0/10, fd7a:115c:a1e0::/48).</summary>
    public static string[] TailscaleAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(a => a.Address))
            .Where(IsTailscale)
            .Select(a => a.ToString())
            .Distinct()
            .OrderBy(a => a.Contains(':'))
            .ToArray();

    private static bool IsTailscale(IPAddress a)
    {
        var b = a.GetAddressBytes();
        return a.AddressFamily == AddressFamily.InterNetwork
            ? b[0] == 100 && (b[1] & 0xC0) == 64
            : b.Length == 16 && b[0] == 0xfd && b[1] == 0x7a && b[2] == 0x11 && b[3] == 0x5c && b[4] == 0xa1 && b[5] == 0xe0;
    }

    /// <summary>
    /// Let friends in on one of this machine's Tailscale addresses, or stop: rewrites server.host
    /// in Config.json (it applies on the next start - see <see cref="Restart"/>). Null when
    /// saved, else why not.
    /// </summary>
    public string? SetHosting(bool on, string? address)
    {
        if (on && (address is null || !TailscaleAddresses().Contains(address)))
            return "That isn't one of this computer's Tailscale addresses. Is Tailscale on?";
        var path = Path.Combine(config.Root, "Config.json");
        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        if (root?["server"] is not JsonObject server)
            return "Config.json has no server section.";
        server["host"] = on ? $"127.0.0.1, {address}" : "127.0.0.1";
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + "\n");
        File.Move(tmp, path, overwrite: true);
        logger.LogInformation("Launcher: hosting {State}", on ? $"on at {address}" : "off");
        return null;
    }

    /// <summary>
    /// Start the app again in the background (it waits for this server to stop), then stop.
    /// Null when restarting, else a note for the window.
    /// </summary>
    public string? Restart()
    {
        var app = config.ResolvePath(config.Launcher.AppPath);
        if (!File.Exists(app))
            return "Saved. Close and reopen the app to apply it.";
        Process.Start(new ProcessStartInfo(app, "--restart")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(app)!,
        });
        Stop();
        return null;
    }

    /// <summary>Stop the server a moment from now, so the request asking for it still gets its answer.</summary>
    public void Stop() => _ = Task.Delay(400).ContinueWith(_ => lifetime.StopApplication());

    // ---------------------------------------------------------------- staying up only while needed

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.Launcher.Enabled || config.Launcher.IdleStopSeconds <= 0)
            return;
        var idle = TimeSpan.FromSeconds(config.Launcher.IdleStopSeconds);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                if (DateTime.UtcNow - _lastSeen < idle || HostingAddress is not null || Busy || GameRunning())
                    continue;
                logger.LogInformation("Launcher: window closed and Limbus not running - stopping the server");
                lifetime.StopApplication();
                return;
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}
