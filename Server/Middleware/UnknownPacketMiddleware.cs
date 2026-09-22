using System.Collections.Concurrent;

namespace Server.Middleware;

/// <summary>
/// Names the packets the client asks for that this server has no handler for.
///
/// An unhandled /api/ route falls through to a bare 404 with an empty body. The
/// client then tries to deserialise that empty body into the response schema it
/// expected and throws, which shows up in myosotis.log as a wall of
/// "_responseEvent.Invoke threw" with no indication of WHICH feature died.
/// Logging the packet name once turns that into a readable list of what is
/// actually missing.
/// </summary>
public sealed class UnknownPacketMiddleware(RequestDelegate next, ILogger<UnknownPacketMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, byte> Seen = new();

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode != StatusCodes.Status404NotFound)
            return;

        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.Ordinal))
            return;

        // Once per packet per run - the client retries these constantly.
        if (Seen.TryAdd(path, 0))
            logger.LogWarning(
                "Unimplemented packet: {Packet} - the client will throw on the empty reply "
                + "and whatever feature needs it will not work", path["/api/".Length..]);
    }
}
