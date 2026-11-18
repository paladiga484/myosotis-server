using System.Text.Json;
using System.Text.RegularExpressions;
using Common;

namespace Server.Tweak;

/// <summary>
/// How the mod menu and the app window look: a background (black, white, a colour, or a picture
/// of the player's own) and one accent colour. Kept in theme.json beside Config.json, the picture
/// in theme-picture; both pages read it from /tweak/api/theme.
/// </summary>
public static partial class Look
{
    public sealed record Theme(string Bg = "black", string Colour = "#202020", string Accent = "#7fa7d4",
        string Tone = "dark", long Picture = 0);

    public const int MaxPicture = 8 << 20;

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex Hex();

    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Theme Load(Config config)
    {
        try
        {
            var t = JsonSerializer.Deserialize<Theme>(File.ReadAllText(config.ResolvePath("theme.json")), Json);
            if (t is not null && Valid(t))
                return t;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
        }
        return new Theme();
    }

    public static bool Valid(Theme t) =>
        t.Bg is "black" or "white" or "colour" or "picture" && t.Tone is "dark" or "light"
        && Hex().IsMatch(t.Colour) && Hex().IsMatch(t.Accent);

    public static void Save(Config config, Theme t) =>
        File.WriteAllText(config.ResolvePath("theme.json"), JsonSerializer.Serialize(t, Json));

    /// <summary>The picture's content type from its first bytes, or null if it isn't PNG, JPEG or WebP.</summary>
    public static string? PictureType(ReadOnlySpan<byte> b) =>
        b.Length > 12 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) ? "image/png"
        : b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : b.Length > 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8) ? "image/webp"
        : null;

    /// <summary>The flat colour tokens both pages are built on; the script below fills them in.</summary>
    public const string Tokens = """
  :root{
    color-scheme:dark;
    --bg:#000; --fg:#f2f2f2; --accent:#7fa7d4; --on-accent:#000;
    --muted:color-mix(in srgb,var(--fg) 62%,var(--bg));
    --line:color-mix(in srgb,var(--fg) 14%,var(--bg));
    --panel:color-mix(in srgb,var(--fg) 5%,var(--bg));
    --hover:color-mix(in srgb,var(--fg) 8%,transparent);
    --danger:#e5484d;
    --font:"Segoe UI Variable Text","Segoe UI",system-ui,"Noto Sans","Cantarell",sans-serif;
  }
  body{background:var(--bg) center/cover no-repeat fixed;color:var(--fg);font:400 14px/20px var(--font)}
""";

    /// <summary>applyTheme(t) and loadTheme(): read /tweak/api/theme and set the tokens. Text is
    /// black or white, whichever reads on the background; the same for text on the accent.</summary>
    public const string Script = """
const lum = hex => { const n = parseInt(hex.slice(1), 16), c = [n >> 16, (n >> 8) & 255, n & 255]
  .map(v => { v /= 255; return v <= .03928 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4; });
  return .2126 * c[0] + .7152 * c[1] + .0722 * c[2]; };
let theme = null;
function applyTheme(t){
  theme = t;
  const bg = t.bg === "white" ? "#ffffff" : t.bg === "colour" ? t.colour
           : t.bg === "picture" ? (t.tone === "light" ? "#ffffff" : "#000000") : "#000000";
  const light = lum(bg) > .35, r = document.documentElement.style;
  r.setProperty("--bg", bg);
  r.setProperty("--fg", light ? "#111111" : "#f2f2f2");
  r.setProperty("--accent", t.accent);
  r.setProperty("--on-accent", lum(t.accent) > .35 ? "#000000" : "#ffffff");
  r.colorScheme = light ? "light" : "dark";
  const pic = t.bg === "picture" && t.picture;
  document.body.classList.toggle("picture", !!pic);
  document.body.style.backgroundImage = pic ? `url("/tweak/theme/picture?v=${t.picture}")` : "";
}
const loadTheme = () => fetch("/tweak/api/theme").then(r => r.ok ? r.json() : null).then(t => t && applyTheme(t)).catch(() => {});
""" + "\n";   // a raw string drops its last newline; the page's own script follows on the next line
}
