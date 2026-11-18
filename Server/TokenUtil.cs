using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Server;

/// <summary>
/// Auth codes are JWTs signed with HMAC-SHA256 under a key only this server knows
/// (<see cref="Common.AuthConfig.KeyPath"/>, made on first start). The payload names the uid, so
/// a code is only believed when its signature checks out - otherwise anyone who can reach the
/// server could write another player's uid into one and play as them.
/// </summary>
public static class TokenUtil
{
    private const long LifetimeSeconds = 86400;
    private const int KeyBytes = 32;

    private static byte[] _key = [];

    /// <summary>The signing key at <paramref name="path"/>, created there if it does not exist yet.</summary>
    public static void Init(string path)
    {
        if (!File.Exists(path))
        {
            var fresh = RandomNumberGenerator.GetBytes(KeyBytes);
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            using (var stream = new FileStream(path, options))
            using (var writer = new StreamWriter(stream))
                writer.Write(Convert.ToBase64String(fresh));
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(File.ReadAllText(path).Trim());
        }
        catch (FormatException)
        {
            throw new InvalidDataException($"{path} is not a signing key; delete it and a new one is made on start.");
        }

        if (key.Length < KeyBytes)
            throw new InvalidDataException($"{path} holds a key shorter than {KeyBytes} bytes; delete it and a new one is made on start.");
        _key = key;
    }

    public static string Mint(long uid)
    {
        var now = Common.TimeUtil.UnixNow();
        const string header = """{"alg":"HS256","typ":"JWT"}""";
        var payload = $$"""{"sub":"{{uid}}","exp":{{now + LifetimeSeconds}},"iat":{{now}},"itn":"{{uid}}"}""";
        var signed = $"{Base64UrlEncode(header)}.{Base64UrlEncode(payload)}";
        return $"{signed}.{Base64UrlEncode(Sign(signed))}";
    }

    public static long? UidFromToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return null;

        var signature = Base64UrlDecode(parts[2]);
        if (signature is null || !CryptographicOperations.FixedTimeEquals(signature, Sign($"{parts[0]}.{parts[1]}")))
            return null;

        var payloadBytes = Base64UrlDecode(parts[1]);
        if (payloadBytes is null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payloadBytes);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("sub", out var sub)
                || sub.ValueKind != JsonValueKind.String)
                return null;
            return long.TryParse(sub.GetString(), out var uid) ? uid : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[] Sign(string signed)
    {
        if (_key.Length == 0)
            throw new InvalidOperationException("TokenUtil.Init has not loaded the signing key.");
        return HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(signed));
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
