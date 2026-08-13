using System.Collections.Concurrent;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Database;

public sealed class DatabaseService(IServiceScopeFactory scopeFactory, ILogger<DatabaseService> logger)
{
    private readonly ConcurrentDictionary<long, Lazy<Task<UserDataSnapshot>>> _cache = new();

    public async Task InitializeAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyosotisDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        logger.LogInformation("Database initialized.");
    }

    public async Task<UserDataSnapshot> GetUserDataSnapshotAsync(long uid)
    {
        var lazy = _cache.GetOrAdd(uid, id =>
            new Lazy<Task<UserDataSnapshot>>(() => BuildUserDataSnapshotAsync(id)));
        return await lazy.Value;
    }

    public void Invalidate(long uid) => _cache.TryRemove(uid, out _);

    private async Task<UserDataSnapshot> BuildUserDataSnapshotAsync(long uid)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var users = sp.GetRequiredService<UserRepository>();
        var personalities = sp.GetRequiredService<PersonalityRepository>();
        var egos = sp.GetRequiredService<EgoRepository>();
        var items = sp.GetRequiredService<ItemRepository>();
        var formations = sp.GetRequiredService<FormationRepository>();
        var announcers = sp.GetRequiredService<AnnouncerRepository>();
        var profiles = sp.GetRequiredService<ProfileRepository>();
        var banners = sp.GetRequiredService<BannerRepository>();
        var tickets = sp.GetRequiredService<TicketRepository>();

        var ticketData = await tickets.GetAsync(uid);

        return new UserDataSnapshot(
            await users.GetAsync(uid),
            await personalities.ListAsync(uid),
            await egos.ListAsync(uid),
            await items.ListAsync(uid),
            await formations.ListAsync(uid),
            await announcers.GetAsync(uid),
            await profiles.GetAsync(uid),
            await banners.ListAsync(uid),
            ticketData.Left,
            ticketData.Right,
            ticketData.EgoBg);
    }
}
