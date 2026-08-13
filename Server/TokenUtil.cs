using System.Text;
using System.Text.Json;

namespace Server;

public static class TokenUtil
{
    private const long LifetimeSeconds = 86400;

    public static string Mint(long uid)
    {
        var now = Common.TimeUtil.UnixNow();
        const string header = """{"alg":"HS256","typ":"JWT"}""";
        var payload = $$"""{"sub":"{{uid}}","exp":{{now + LifetimeSeconds}},"iat":{{now}},"itn":"{{uid}}"}""";
        return $"{Base64UrlEncode(header)}.{Base64UrlEncode(payload)}.{Base64UrlEncode(new byte[32])}";
    }

    public static long? UidFromToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length < 2)
            return null;

        var payloadBytes = Base64UrlDecode(parts[1]);
        if (payloadBytes is null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payloadBytes);
            var sub = doc.RootElement.GetProperty("sub").GetString();
            return sub is not null && long.TryParse(sub, out var uid) ? uid : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Base64UrlEncode(string value) => Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? Base64UrlDecode(string value)
    {
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded += (padded.Length % 4) switch
            {
                2 => "==",
                3 => "=",
                _ => "",
            };
            return Convert.FromBase64String(padded);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
