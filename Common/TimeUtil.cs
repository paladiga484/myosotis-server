namespace Common;

public static class TimeUtil
{
    public static long UnixNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static string ToIso(long unix) =>
        DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

    public static string IsoNow() => ToIso(UnixNow());
}
