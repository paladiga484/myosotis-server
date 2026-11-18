using Common;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class UserRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<UserRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<UserInfo?> GetAsync(long uid)
    {
        var user = await Db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uid == uid);
        if (user is null)
            return null;

        return new UserInfo
        {
            uid = user.Uid,
            level = user.Level,
            exp = user.Exp,
            stamina = user.Stamina,
            last_stamina_recover = TimeUtil.ToIso(user.LastStaminaRecover),
        };
    }

    public async Task<bool> UpdateAsync(long uid, int? level = null, int? exp = null, int? stamina = null)
    {
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Uid == uid);
        if (user is null)
            return true;

        if (level is not null) user.Level = level.Value;
        if (exp is not null) user.Exp = exp.Value;
        if (stamina is not null) user.Stamina = stamina.Value;
        return await SaveAsync(uid);
    }

    public async Task TouchLoginAsync(long uid)
    {
        var user = await Db.Users.FirstOrDefaultAsync(u => u.Uid == uid);
        if (user is null)
            return;

        user.LastLoginAt = TimeUtil.UnixNow();
        await Db.SaveChangesAsync();
        DatabaseService.Invalidate(uid);
    }
}
