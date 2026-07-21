using System.Text;

// TODO: use a custom logger provider. The default one is ugly

namespace Server;

public class LoggerMiddleware(RequestDelegate next, ILogger<LoggerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        var requestBody = await ReadBodyAsync(context.Request.Body);
        context.Request.Body.Position = 0;
        
        logger.LogInformation("\n{Method} {Route}\n{Body}\n", context.Request.Method, context.Request.Path, string.IsNullOrEmpty(requestBody) ? "<none>" : requestBody);

        var originalBodyStream = context.Response.Body;
        using var responseBodyMemory = new MemoryStream();
        context.Response.Body = responseBodyMemory;

        try
        {
            await next(context);

            responseBodyMemory.Position = 0;
            var responseBody = await ReadBodyAsync(responseBodyMemory);
            responseBodyMemory.Position = 0;
            await responseBodyMemory.CopyToAsync(originalBodyStream);

            logger.LogInformation("\nSTATUS {Status}\n{Body}\n", context.Response.StatusCode, string.IsNullOrEmpty(responseBody) ? "<none>" : responseBody);
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
