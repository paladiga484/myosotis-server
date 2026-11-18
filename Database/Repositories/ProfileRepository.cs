using Common;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Types.Server;

namespace Database.Repositories;

public sealed class ProfileRepository(
    MyosotisDbContext db,
    DatabaseService databaseService,
    ILogger<ProfileRepository> logger) : RepositoryBase(db, databaseService, logger)
{

    public async Task<UserPublicProfileWithSupportersFormat?> GetAsync(long uid)
    {
        var profile = await Db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Uid == uid);
        if (profile is null)
            return null;

        var banners = await Db.ProfileBanners.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.Idx)
            .ToListAsync();
        var supports = await Db.ProfileSupportPersonalities.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.Idx)
            .ToListAsync();
        var supportEgos = await Db.ProfileSupportEgos.AsNoTracking()
            .Where(x => x.Uid == uid)
            .OrderBy(x => x.Idx)
            .ThenBy(x => x.EgoIdx)
            .ToListAsync();

        return new UserPublicProfileWithSupportersFormat
        {
            public_uid = profile.PublicUid ?? "",
            illust_id = profile.IllustId ?? 0,
            illust_gacksung_level = profile.IllustGacksungLevel ?? 0,
            leftborder_id = profile.LeftBorderId ?? 0,
            rightborder_id = profile.RightBorderId ?? 0,
            egobackground_id = profile.EgoBackgroundId ?? 0,
            sentence_id = profile.SentenceId ?? 0,
            word_id = profile.WordId ?? 0,
            level = profile.Level ?? 0,
            date = profile.Date is long date ? TimeUtil.ToIso(date) : "",
            banners = banners.Select(b => new UserPublicBannerFormat
            {
                id = b.Id,
                value = b.Value,
                value2 = b.Value2,
                value3 = b.Value3,
                value4 = b.Value4,
                value5 = b.Value5,
                idx = b.Idx,
            }).ToList(),
            support_personalities = supports.Select(s => new SupportPersonalitySlotFormat
            {
                idx = s.Idx,
                pid = s.Pid,
                l = s.L,
                gl = s.Gl,
                gi = s.Gi,
                sid = s.Sid,
                egos = supportEgos.Where(e => e.Idx == s.Idx)
                    .Select(e => new ProfileEgoContainIndexFormat { idx = e.EgoIdx, id = e.Id, g = e.G })
                    .ToList(),
            }).ToList(),
        };
    }

    public async Task<bool> UpdateAsync(
        long uid,
        int? illustId = null,
        int? illustGacksungLevel = null,
        int? leftBorderId = null,
        int? rightBorderId = null,
        int? egoBackgroundId = null,
        int? sentenceId = null,
        int? wordId = null,
        IReadOnlyList<UserPublicBannerFormat>? banners = null,
        IReadOnlyList<SupportPersonalitySlotFormat>? supports = null)
    {
        var profile = await Db.UserProfiles.FirstOrDefaultAsync(x => x.Uid == uid);
        if (profile is not null)
        {
            if (illustId is not null) profile.IllustId = illustId;
            if (illustGacksungLevel is not null) profile.IllustGacksungLevel = illustGacksungLevel;
            if (leftBorderId is not null) profile.LeftBorderId = leftBorderId;
            if (rightBorderId is not null) profile.RightBorderId = rightBorderId;
            if (egoBackgroundId is not null) profile.EgoBackgroundId = egoBackgroundId;
            if (sentenceId is not null) profile.SentenceId = sentenceId;
            if (wordId is not null) profile.WordId = wordId;
        }

        if (banners is not null)
        {
            Db.ProfileBanners.RemoveRange(Db.ProfileBanners.Where(x => x.Uid == uid));
            Db.ProfileBanners.AddRange(banners.Select(b => new ProfileBanner
            {
                Uid = uid,
                Id = b.id,
                Value = b.value,
                Value2 = b.value2,
                Value3 = b.value3,
                Value4 = b.value4,
                Value5 = b.value5,
                Idx = b.idx,
            }));
        }

        if (supports is not null)
        {
            Db.ProfileSupportPersonalities.RemoveRange(Db.ProfileSupportPersonalities.Where(x => x.Uid == uid));
            Db.ProfileSupportEgos.RemoveRange(Db.ProfileSupportEgos.Where(x => x.Uid == uid));
            foreach (var support in supports)
            {
                Db.ProfileSupportPersonalities.Add(new ProfileSupportPersonality
                {
                    Uid = uid,
                    Idx = support.idx,
                    Pid = support.pid,
                    L = support.l,
                    Gl = support.gl,
                    Gi = support.gi,
                    Sid = support.sid,
                });
                foreach (var ego in support.egos)
                    Db.ProfileSupportEgos.Add(new ProfileSupportEgo
                    {
                        Uid = uid,
                        Idx = support.idx,
                        EgoIdx = ego.idx,
                        Id = ego.id,
                        G = ego.g,
                    });
            }
        }

        return await SaveAsync(uid);
    }
}
