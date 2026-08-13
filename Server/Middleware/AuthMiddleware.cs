using System.Text.Json;
using Common;
using Types.Server;

namespace Server.Middleware;

public sealed class AuthMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/", StringComparison.Ordinal) &&
            !path.StartsWith("/iap/", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        long claimedUid = 0;
        string token = "";
        try
        {
            var request = JsonSerializer.Deserialize<HttpRequestFormat<JsonElement>>(body, JsonOptions);
            claimedUid = request?.userAuth?.uid ?? 0;
            token = request?.userAuth?.authCode ?? "";
        }
        catch (JsonException)
        {
            // treated as an unauthenticated request below
        }

        var config = context.RequestServices.GetRequiredService<Config>();
        long uid;
        if (config.Auth.RequireToken)
        {
            var resolved = TokenUtil.UidFromToken(token);
            if (resolved is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            uid = resolved.Value;
        }
        else
        {
            uid = claimedUid;
        }

        context.Items["Uid"] = uid;
        await next(context);
    }
}
