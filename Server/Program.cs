using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
TokenUtil.Init(config.ResolvePath(config.Auth.KeyPath));

string? gameDataNote = null;
if (config.Launcher.Enabled && config.Launcher.ImportGameData)
{
    // the app shows this file if the server stops before it is up
    var errorFile = config.ResolvePath("game-data-error.txt");
    File.Delete(errorFile);
    var why = Server.Launcher.GameData.Prepare(config, out gameDataNote);
    if (why is not null)
    {
        File.WriteAllText(errorFile, why);
        Console.Error.WriteLine(why);
        Environment.Exit(3);
    }
}

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
    config.StaticDataRoot(),
    sp.GetRequiredService<ILogger<StaticDataService>>()));

builder.Services.AddSingleton(sp => new MirrorDungeonData(
    config.StaticDataRoot(),
    sp.GetRequiredService<ILogger<MirrorDungeonData>>()));
builder.Services.AddSingleton<Server.MirrorDungeon.MapGenerator>();
builder.Services.AddSingleton<Server.MirrorDungeon.ThemeFloorPicker>();

builder.Services.AddSingleton(sp => new NameService(
    config.StaticDataRoot(),
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
builder.Services.AddSingleton<Server.Launcher.LauncherService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Server.Launcher.LauncherService>());

builder.Services.AddControllers();

var app = builder.Build();

// limbus-skins' Black Forest unlock follows what is open, from the start
Server.BlackForest.For(config);

if (gameDataNote is not null)
    Log.Information("{Note}", gameDataNote);
Log.Information("Supported versions: {Versions}", string.Join(", ", config.Server.Versions));
Log.Information("Require token: {RequireToken}", config.Auth.RequireToken);
Log.Information("Sync command allowed: {AllowSyncCommand}", config.Command.AllowSyncCommand);
Log.Information("Reset DB command allowed: {AllowResetDbCommand}", config.Command.AllowResetDbCommand);
if (config.Launcher.Enabled)
    Log.Information("App window at /launcher (stops {Idle}s after it closes, unless Limbus is running)",
        config.Launcher.IdleStopSeconds);

var listen = ListenAddresses(config);
foreach (var address in listen)
    app.Urls.Add($"http://{(address.AddressFamily == AddressFamily.InterNetworkV6 ? $"[{address}]" : address)}:{config.Server.Port}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var url in app.Urls)
        Log.Information("Listening on {Url}", url);
});

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
app.Run();

// The configured addresses this server can listen on now. Loopback always works; any other
// address lets other machines in, so it has to be one this machine really has (its Tailscale or
// WireGuard address) and tokens have to be required. Every-interface binds are refused outright.
static List<IPAddress> ListenAddresses(Config config)
{
    var mine = NetworkInterface.GetAllNetworkInterfaces()
        .SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(a => a.Address))
        .ToHashSet();
    var result = new List<IPAddress>();
    foreach (var host in config.Server.Hosts())
    {
        IPAddress? address = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? IPAddress.Loopback : null;
        if (address is null && !IPAddress.TryParse(host.Trim('[', ']'), out address))
            throw new InvalidOperationException(
                $"server.host: '{host}' is not an IP address. List addresses, e.g. \"127.0.0.1, 100.64.0.7\".");
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            throw new InvalidOperationException(
                $"server.host: '{host}' would listen on every network this machine joins. "
                + "Give this machine's Tailscale or WireGuard address instead.");
        if (IPAddress.IsLoopback(address))
        {
            result.Add(address);
            continue;
        }
        if (!config.Auth.RequireToken)
            throw new InvalidOperationException(
                $"server.host: '{host}' lets other machines in, so auth.requireToken has to be true.");
        if (!mine.Contains(address))
        {
            Log.Warning("{Host} is not an address of this machine right now (is Tailscale up?); "
                + "not listening on it until the server is restarted", host);
            continue;
        }
        result.Add(address);
    }
    if (result.Count == 0)
        throw new InvalidOperationException("server.host: none of the listed addresses can be listened on.");
    return result.Distinct().ToList();
}
