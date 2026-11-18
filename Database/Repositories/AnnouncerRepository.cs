using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class AnnouncerRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<AnnouncerRepository> logger) : RepositoryBase(db, databaseService, logger)
{
    public async Task<AnnouncerFormat> GetAsync(long uid)
    {
        var owned = await Db.UserAnnouncers.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => x.AnnouncerId)
            .OrderBy(id => id)
            .ToListAsync();
        var state = await Db.UserAnnouncerStates.AsNoTracking().FirstOrDefaultAsync(x => x.Uid == uid);
        var presets = await Db.AnnouncerPresets.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.PresetId)
            .ToListAsync();
        var entries = await Db.AnnouncerPresetEntries.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.PresetId)
            .ThenBy(x => x.Idx)
            .ToListAsync();

        return new AnnouncerFormat
        {
            announcer_ids = owned,
            cur_preset_id = state?.CurPresetId ?? 0,
            presets = presets.Select(p => new AnnouncerPresetFormat
            {
                presetId = p.PresetId,
                presetAnnouncerIds = entries.Where(e => e.PresetId == p.PresetId).Select(e => e.AnnouncerId).ToList(),
            }).ToList(),
        };
    }

    public async Task<bool> InsertPresetAsync(long uid, int presetId, IReadOnlyList<int> presetAnnouncerIds)
    {
        if (!await Db.AnnouncerPresets.AnyAsync(x => x.Uid == uid && x.PresetId == presetId))
            Db.AnnouncerPresets.Add(new AnnouncerPreset { Uid = uid, PresetId = presetId });

        Db.AnnouncerPresetEntries.RemoveRange(Db.AnnouncerPresetEntries.Where(x => x.Uid == uid && x.PresetId == presetId));
        for (int i = 0; i < presetAnnouncerIds.Count; i++)
            Db.AnnouncerPresetEntries.Add(new AnnouncerPresetEntry
            {
                Uid = uid,
                PresetId = presetId,
                Idx = i,
                AnnouncerId = presetAnnouncerIds[i],
            });

        return await SaveAsync(uid);
    }

    public async Task<bool> SetCurrentPresetAsync(long uid, int presetId)
    {
        var state = await Db.UserAnnouncerStates.FirstOrDefaultAsync(x => x.Uid == uid);
        if (state is null)
            Db.UserAnnouncerStates.Add(new UserAnnouncerState { Uid = uid, CurPresetId = presetId });
        else
            state.CurPresetId = presetId;

        return await SaveAsync(uid);
    }

    public async Task<HashSet<int>> GetIdsAsync(long uid) =>
        (await Db.UserAnnouncers.AsNoTracking()
            .Where(x => x.Uid == uid)
            .Select(x => x.AnnouncerId)
            .ToListAsync()).ToHashSet();

    public async Task<AnnouncerFormat> SyncNewAsync(long uid, IReadOnlyCollection<int> allIds)
    {
        var existing = await GetIdsAsync(uid);
        var missing = allIds.Where(id => !existing.Contains(id)).OrderBy(id => id).ToList();
        if (missing.Count > 0)
        {
            Db.UserAnnouncers.AddRange(missing.Select(id => new UserAnnouncer { Uid = uid, AnnouncerId = id }));
            await SaveAsync(uid);
        }

        if (!await Db.AnnouncerPresets.AnyAsync(x => x.Uid == uid))
        {
            var firstTen = (await GetIdsAsync(uid)).OrderBy(id => id).Take(10).ToList();
            await InsertPresetAsync(uid, 1, firstTen);
            await SetCurrentPresetAsync(uid, 1);
        }

        return await GetAsync(uid);
    }
}
