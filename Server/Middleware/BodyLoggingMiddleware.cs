using System.Text;

namespace Server.Middleware;

public sealed class BodyLoggingMiddleware(RequestDelegate next, ILogger<BodyLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBody = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        logger.LogInformation(">> {Method} {Route}", context.Request.Method, context.Request.Path);
        logger.LogInformation(
            "REQUEST BODY: {Body}",
            string.IsNullOrEmpty(requestBody) ? "<none>" : requestBody);

        var originalBodyStream = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await next(context);

            responseBuffer.Position = 0;
            var responseBody = await ReadBodyAsync(responseBuffer);
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBodyStream);

            logger.LogInformation(
                "STATUS {Status}: {Body}",
                context.Response.StatusCode,
                string.IsNullOrEmpty(responseBody) ? "<none>" : responseBody);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static async Task<string> ReadBodyAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
