using Common;
using Database;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Resource;
using Server;
using Server.Middleware;
using Serilog;
using Serilog.Events;

var config = Config.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, cfg) => cfg
    .MinimumLevel.Is(Enum.Parse<LogEventLevel>(config.Logging.MinimumLevel, ignoreCase: true))
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    // Kept on deliberately: server.log is truncated on every restart, so without a durable sink
    // the record of what the client asked for - especially "Unimplemented packet" lines - is
    // gone the moment the server bounces, which is exactly when you need it.
    .WriteTo.File(
        config.ResolvePath("logs/myosotis-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    );

builder.Services.AddSingleton(config);

builder.Services.AddSingleton<DatabaseService>();

builder.Services.AddSingleton(sp => new StaticDataService(
    config.ResolvePath(config.StaticData.Path),
    sp.GetRequiredService<ILogger<StaticDataService>>()));

builder.Services.AddSingleton(sp => new MirrorDungeonData(
    config.ResolvePath(config.StaticData.Path),
    sp.GetRequiredService<ILogger<MirrorDungeonData>>()));
builder.Services.AddSingleton<Server.MirrorDungeon.MapGenerator>();
builder.Services.AddSingleton<Server.MirrorDungeon.ThemeFloorPicker>();

builder.Services.AddSingleton(sp => new NameService(
    config.ResolvePath(config.StaticData.Path),
    sp.GetRequiredService<ILogger<NameService>>()));
builder.Services.AddDbContext<MyosotisDbContext>(o =>
    o.UseSqlite($"Data Source={config.ResolvePath(config.Database.Path)};Foreign Keys=True"));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<PersonalityRepository>();
builder.Services.AddScoped<EgoRepository>();
builder.Services.AddScoped<ItemRepository>();
builder.Services.AddScoped<FormationRepository>();
builder.Services.AddScoped<AnnouncerRepository>();
builder.Services.AddScoped<BannerRepository>();
builder.Services.AddScoped<TicketRepository>();
builder.Services.AddScoped<ProfileRepository>();
builder.Services.AddScoped<RailwayRepository>();
builder.Services.AddScoped<MirrorDungeonRepository>();
builder.Services.AddScoped<AccountService>();

builder.Services.AddControllers();

var app = builder.Build();

Log.Information("Supported versions: {Versions}", string.Join(", ", config.Server.Versions));
Log.Information("Require token: {RequireToken}", config.Auth.RequireToken);
Log.Information("Sync command allowed: {AllowSyncCommand}", config.Command.AllowSyncCommand);
Log.Information("Reset DB command allowed: {AllowResetDbCommand}", config.Command.AllowResetDbCommand);

app.Lifetime.ApplicationStarted.Register(() =>
    Log.Information("Listening on http://{Host}:{Port}", config.Server.Host, config.Server.Port));

app.Lifetime.ApplicationStopping.Register(() =>
    Log.Information("Application is shutting down..."));

app.Lifetime.ApplicationStopped.Register(() =>
    Log.Information("Application stopped."));

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    await database.InitializeAsync();

    var staticData = scope.ServiceProvider.GetRequiredService<StaticDataService>();
    await staticData.LoadAllAsync();
    var nameService = scope.ServiceProvider.GetRequiredService<NameService>();
    await nameService.LoadAllAsync();

    var mirrorData = scope.ServiceProvider.GetRequiredService<MirrorDungeonData>();
    await mirrorData.LoadAllAsync();
}

if (config.Logging.LogBodies)
    app.UseMiddleware<BodyLoggingMiddleware>();

app.UseMiddleware<UnknownPacketMiddleware>();
app.UseMiddleware<AuthMiddleware>();
app.MapControllers();
app.Run($"http://{config.Server.Host}:{config.Server.Port}");
