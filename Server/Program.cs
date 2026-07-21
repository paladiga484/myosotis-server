namespace Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        var app = builder.Build();
        app.UseMiddleware<LoggerMiddleware>();
        app.MapControllers();
        app.Run();
    }
}
