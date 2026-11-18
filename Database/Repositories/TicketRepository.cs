using Common;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class TicketRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<TicketRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public const string Left = "LEFT";
    public const string Right = "RIGHT";
    public const string EgoBg = "EGOBG";

    public async Task<(List<UserProfileBorderFormat> Left, List<UserProfileBorderFormat> Right, List<UserProfileEgobackgroundFormat> EgoBg)> GetAsync(long uid)
    {
        var rows = await Db.ProfileTickets.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return (
            rows.Where(r => r.TicketType == Left).Select(r => new UserProfileBorderFormat { id = r.Id, date = TimeUtil.ToIso(r.Date) }).ToList(),
            rows.Where(r => r.TicketType == Right).Select(r => new UserProfileBorderFormat { id = r.Id, date = TimeUtil.ToIso(r.Date) }).ToList(),
            rows.Where(r => r.TicketType == EgoBg).Select(r => new UserProfileEgobackgroundFormat { id = r.Id, date = TimeUtil.ToIso(r.Date) }).ToList());
    }

    public async Task<HashSet<(string Type, int Id)>> GetKeysAsync(long uid) =>
        (await Db.ProfileTickets.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => new { x.TicketType, x.Id })
            .ToListAsync())
        .Select(x => (x.TicketType, x.Id))
        .ToHashSet();

    public async Task SyncNewAsync(
        long uid, IReadOnlyCollection<int> leftIds, IReadOnlyCollection<int> rightIds, IReadOnlyCollection<int> egoBgIds)
    {
        var existing = await GetKeysAsync(uid);
        var now = TimeUtil.UnixNow();

        void AddMissing(string type, IEnumerable<int> ids)
        {
            foreach (var id in ids.Where(id => !existing.Contains((type, id))))
                Db.ProfileTickets.Add(new ProfileTicket { Uid = uid, TicketType = type, Id = id, Date = now });
        }

        AddMissing(Left, leftIds);
        AddMissing(Right, rightIds);
        AddMissing(EgoBg, egoBgIds);
        await SaveAsync(uid);
    }
}
