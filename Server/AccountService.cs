using Common;
using Database;
using Database.Entities;
using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Resource;

namespace Server;

public sealed class AccountService(
    Config config,
    MyosotisDbContext db,
    DatabaseService databaseService,
    StaticDataService staticData,
    NameService names,
    ILogger<AccountService> logger)
{
    public async Task<SignInResult> SignInAsync(string credential, string accountType)
    {
        credential = credential.Trim();
        if (credential.Length == 0)
            return new SignInResult(0, "");

        SignInResult result;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Credential == credential);
        if (user is not null)
        {
            var authCode = TokenUtil.Mint(user.Uid);
            user.LastLoginAt = TimeUtil.UnixNow();
            await db.SaveChangesAsync();
            databaseService.Invalidate(user.Uid);
            result = new SignInResult(user.Uid, authCode);
        }
        else
        {
            result = await CreateUserAsync(credential, accountType);
        }

        logger.LogInformation("User {Uid} logged in ({AccountType})", result.Uid, accountType);
        return result;
    }

    public async Task<SignInResult> CreateUserAsync(string credential, string accountType)
    {
        var now = TimeUtil.UnixNow();
        var user = new User
        {
            Credential = credential,
            AccountType = accountType,
            Level = config.Seed.UserLevel,
            Exp = 0,
            Stamina = config.Seed.Stamina,
            LastStaminaRecover = now,
            LastLoginAt = now,
            RegisterDate = now,
        };

        await using var tx = await db.Database.BeginTransactionAsync();
        db.Users.Add(user);
        await db.SaveChangesAsync(); // uid generated here

        SeedOwned(user.Uid, now);
        await SeedFormationsAsync(user.Uid);
        await tx.CommitAsync();

        var authCode = TokenUtil.Mint(user.Uid);
        databaseService.Invalidate(user.Uid);
        return new SignInResult(user.Uid, authCode);
    }

    public async Task ResetAsync(long uid)
    {
        var now = TimeUtil.UnixNow();
        await using var tx = await db.Database.BeginTransactionAsync();

        db.UserPersonalities.RemoveRange(db.UserPersonalities.Where(x => x.Uid == uid));
        db.UserEgos.RemoveRange(db.UserEgos.Where(x => x.Uid == uid));
        db.UserItems.RemoveRange(db.UserItems.Where(x => x.Uid == uid));
        db.Formations.RemoveRange(db.Formations.Where(x => x.Uid == uid));
        db.UserAnnouncers.RemoveRange(db.UserAnnouncers.Where(x => x.Uid == uid));
        db.AnnouncerPresets.RemoveRange(db.AnnouncerPresets.Where(x => x.Uid == uid));
        db.AnnouncerPresetEntries.RemoveRange(db.AnnouncerPresetEntries.Where(x => x.Uid == uid));
        db.UserAnnouncerStates.RemoveRange(db.UserAnnouncerStates.Where(x => x.Uid == uid));
        db.UserBanners.RemoveRange(db.UserBanners.Where(x => x.Uid == uid));
        db.ProfileTickets.RemoveRange(db.ProfileTickets.Where(x => x.Uid == uid));
        db.UserProfiles.RemoveRange(db.UserProfiles.Where(x => x.Uid == uid));
        db.ProfileBanners.RemoveRange(db.ProfileBanners.Where(x => x.Uid == uid));
        db.ProfileSupportPersonalities.RemoveRange(db.ProfileSupportPersonalities.Where(x => x.Uid == uid));
        db.ProfileSupportEgos.RemoveRange(db.ProfileSupportEgos.Where(x => x.Uid == uid));
        db.RailwaySaves.RemoveRange(db.RailwaySaves.Where(x => x.Uid == uid));
        db.RailwaySaveUnits.RemoveRange(db.RailwaySaveUnits.Where(x => x.Uid == uid));
        db.RailwaySaveUnitEgos.RemoveRange(db.RailwaySaveUnitEgos.Where(x => x.Uid == uid));
        db.MirrorDungeonSaves.RemoveRange(db.MirrorDungeonSaves.Where(x => x.Uid == uid));

        var user = await db.Users.FirstOrDefaultAsync(u => u.Uid == uid);
        if (user is not null)
        {
            user.Level = config.Seed.UserLevel;
            user.Exp = 0;
            user.Stamina = config.Seed.Stamina;
            user.LastStaminaRecover = now;
        }

        await db.SaveChangesAsync();
        SeedOwned(uid, now);
        await SeedFormationsAsync(uid);
        await tx.CommitAsync();

        databaseService.Invalidate(uid);
        logger.LogInformation(
            "Reset user {Uid} to profile '{Profile}' at level {Level}",
            uid, config.Seed.Profile, config.Seed.UserLevel);
    }

    /// <summary>Identities granted under the balanced profile: all 12 base, a slice of the rest.</summary>
    private List<int> BalancedPersonalityIds()
    {
        var rank2 = new List<int>();
        var rank3 = new List<int>();
        var result = new List<int>();

        foreach (var id in staticData.PersonalityIds.OrderBy(i => i))
        {
            // The first identity of each sinner - ids ending in 01 - is always included so every
            // sinner is playable from the start.
            if (id % 100 == 1) { result.Add(id); continue; }

            switch (names.ForPersonality(id).Rank)
            {
                case 2: rank2.Add(id); break;
                default: rank3.Add(id); break;
            }
        }

        result.AddRange(rank2.Where((_, i) => i % Math.Max(1, config.Seed.BalancedRank2Every) == 0));
        result.AddRange(rank3.Where((_, i) => i % Math.Max(1, config.Seed.BalancedRank3Every) == 0));
        return result;
    }

    private void SeedOwned(long uid, long now)
    {
        // GrantEverything is the older switch; Profile supersedes it when set to anything else.
        var profile = (config.Seed.Profile ?? "everything").Trim().ToLowerInvariant();
        if (profile == "everything" && !config.Seed.GrantEverything)
            profile = "empty";

        if (profile == "empty")
        {
            logger.LogInformation("Seed profile 'empty' - uid {Uid} starts with nothing", uid);
            return;
        }

        var personalityIds = profile switch
        {
            "starter"  => staticData.PersonalityIds.Where(id => id % 100 == 1).OrderBy(i => i).ToList(),
            "balanced" => BalancedPersonalityIds(),
            _          => staticData.PersonalityIds.ToList(),
        };

        var egoIds = profile switch
        {
            "starter"  => [],
            "balanced" => staticData.EgoIds.OrderBy(i => i)
                              .Where((_, i) => i % Math.Max(1, config.Seed.BalancedEgoEvery) == 0)
                              .ToList(),
            _          => staticData.EgoIds.ToList(),
        };

        logger.LogInformation(
            "Seed profile '{Profile}' for uid {Uid}: {Ids} identities, {Egos} E.G.O., {Items} of each item",
            profile, uid, personalityIds.Count, egoIds.Count, config.Seed.ItemCount);

        foreach (var id in personalityIds)
        {
            db.UserPersonalities.Add(new UserPersonality
            {
                Uid = uid,
                PersonalityId = id,
                Level = config.Seed.PersonalityLevel,
                Exp = 0,
                Gacksung = config.Seed.PersonalityUptie,
                OrderId = ProfileUtil.GetOrderId(id),
                GacksungIllustType = SeedValues.PersonalityGacksungIllustType,
                AcquireTime = now,
            });
        }

        foreach (var id in egoIds)
            db.UserEgos.Add(new UserEgo { Uid = uid, EgoId = id, Gacksung = config.Seed.EgoUptie, AcquireTime = now });

        var itemProfile = (config.Seed.ItemProfile ?? "all").Trim().ToLowerInvariant();
        if (itemProfile != "none")
        {
            var granted = 0;
            foreach (var id in staticData.ItemIds)
            {
                var num = itemProfile == "curated" ? SeedItems.CuratedCount(id) : config.Seed.ItemCount;
                if (num <= 0)
                    continue;

                db.UserItems.Add(new UserItem { Uid = uid, ItemId = id, Num = num });
                granted++;
            }

            logger.LogInformation(
                "Item profile '{ItemProfile}' for uid {Uid}: {Granted} of {Total} item types stocked",
                itemProfile, uid, granted, staticData.ItemIds.Count);
        }

        var owned = staticData.AnnouncerIds.OrderBy(id => id).ToList();
        foreach (var id in owned)
            db.UserAnnouncers.Add(new UserAnnouncer { Uid = uid, AnnouncerId = id });

        db.AnnouncerPresets.Add(new AnnouncerPreset { Uid = uid, PresetId = 1 });
        for (int i = 0; i < Math.Min(10, owned.Count); i++)
            db.AnnouncerPresetEntries.Add(new AnnouncerPresetEntry { Uid = uid, PresetId = 1, Idx = i, AnnouncerId = owned[i] });
        db.UserAnnouncerStates.Add(new UserAnnouncerState { Uid = uid, CurPresetId = 1 });

        foreach (var id in staticData.UserBannerIds)
        {
            var (value, value2) = SeedValues.BannerValue(id);
            db.UserBanners.Add(new UserBanner { Uid = uid, Id = id, Acquiretime = now, Value = value, Value2 = value2 });
        }

        foreach (var id in staticData.BorderLeftIds)
            db.ProfileTickets.Add(new ProfileTicket { Uid = uid, TicketType = TicketRepository.Left, Id = id, Date = now });
        foreach (var id in staticData.BorderRightIds)
            db.ProfileTickets.Add(new ProfileTicket { Uid = uid, TicketType = TicketRepository.Right, Id = id, Date = now });
        foreach (var id in staticData.EgoBgIds)
            db.ProfileTickets.Add(new ProfileTicket { Uid = uid, TicketType = TicketRepository.EgoBg, Id = id, Date = now });

        db.UserProfiles.Add(new UserProfile
        {
            Uid = uid,
            // doesnt work, oh well
            PublicUid = ProfileUtil.RandomLetter() + uid.ToString("D9"),
            IllustId = 10101,   // Yi Sang LCB Sinner - owned in every mode
            IllustGacksungLevel = 1,
            LeftBorderId = -1,
            RightBorderId = -1,
            EgoBackgroundId = -1,
            SentenceId = 1,
            WordId = 1,
            Level = 302,
            Date = now,
        });

        db.RailwaySaves.Add(new RailwaySave
        {
            Uid = uid,
            DungeonId = SeedValues.RailwayDungeonId,
            PrevClearNode = -1,
            LastEnterNodeId = 1,
        });
    }

    // 20 formations * 12 slots
    private async Task SeedFormationsAsync(long uid)
    {
        var formations = Enumerable.Range(0, 20)
            .Select(f => new Formation { Uid = uid, Id = f })
            .ToList();
        db.Formations.AddRange(formations);
        await db.SaveChangesAsync();

        foreach (var formation in formations)
        {
            db.FormationNames.Add(new FormationName { FormationId = formation.FormationId, K = -1, V = -1 });
            for (int j = 0; j < 12; j++)
            {
                db.FormationDetails.Add(new FormationDetail
                {
                    FormationId = formation.FormationId,
                    PersonalityId = 10101 + j * 100,
                    IsParticipated = false,
                    ParticipationOrder = j + 1,
                    SkinId = 0,
                });
                db.FormationEgos.Add(new FormationEgo
                {
                    FormationId = formation.FormationId,
                    PersonalityId = 10101 + j * 100,
                    Idx = 0,
                    EgoId = 20101 + j * 100,
                });
            }
        }

        await db.SaveChangesAsync();
    }
}

public sealed record SignInResult(long Uid, string AuthCode);
