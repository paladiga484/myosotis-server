namespace Server;

public static class HttpContextExtensions
{
    public static long GetUid(this HttpContext context) =>
        context.Items.TryGetValue("Uid", out var value) && value is long uid
            ? uid
            : throw new InvalidOperationException(
                "Authenticated uid missing from HttpContext.Items; AuthMiddleware must run before this handler.");
}
