using Common;

namespace Database.Entities;

public sealed class User
{
    public long Uid { get; set; }
    public string Credential { get; set; } = "";
    public string AccountType { get; set; } = "";
    public int Level { get; set; } = SeedValues.UserLevel;
    public int Exp { get; set; }
    public int Stamina { get; set; } = SeedValues.UserStamina;
    public long LastStaminaRecover { get; set; }
    public long LastLoginAt { get; set; }
    public long RegisterDate { get; set; }
}

public sealed class UserPersonality
{
    public long Uid { get; set; }
    public int PersonalityId { get; set; }
    public int Level { get; set; } = SeedValues.PersonalityLevel;
    public int Exp { get; set; }
    public int Gacksung { get; set; } = SeedValues.PersonalityGacksung;
    public int OrderId { get; set; }
    public int GacksungIllustType { get; set; } = SeedValues.PersonalityGacksungIllustType;
    public long AcquireTime { get; set; }
}

public sealed class UserEgo
{
    public long Uid { get; set; }
    public int EgoId { get; set; }
    public int Gacksung { get; set; } = SeedValues.EgoGacksung;
    public long AcquireTime { get; set; }
}

public sealed class UserItem
{
    public long Uid { get; set; }
    public int ItemId { get; set; }
    public int Num { get; set; }
}

public sealed class UserAnnouncer
{
    public long Uid { get; set; }
    public int AnnouncerId { get; set; }
}

public sealed class AnnouncerPreset
{
    public long Uid { get; set; }
    public int PresetId { get; set; }
}

public sealed class AnnouncerPresetEntry
{
    public long Uid { get; set; }
    public int PresetId { get; set; }
    public int Idx { get; set; }
    public int AnnouncerId { get; set; }
}

public sealed class UserAnnouncerState
{
    public long Uid { get; set; }
    public int CurPresetId { get; set; }
}

public sealed class UserBanner
{
    public long Uid { get; set; }
    public int Id { get; set; }
    public long Acquiretime { get; set; }
    public int Value { get; set; } = -1;
    public int Value2 { get; set; } = -1;
    public int Value3 { get; set; } = -1;
    public int Value4 { get; set; } = -1;
    public int Value5 { get; set; } = -1;
}

public sealed class ProfileTicket
{
    public long Uid { get; set; }
    public string TicketType { get; set; } = "";
    public int Id { get; set; }
    public long Date { get; set; }
}

public sealed class UserProfile
{
    public long Uid { get; set; }
    public string? PublicUid { get; set; }
    public int? IllustId { get; set; }
    public int? IllustGacksungLevel { get; set; }
    public int? LeftBorderId { get; set; }
    public int? RightBorderId { get; set; }
    public int? EgoBackgroundId { get; set; }
    public int? SentenceId { get; set; }
    public int? WordId { get; set; }
    public int? Level { get; set; }
    public long? Date { get; set; }
}

public sealed class ProfileBanner
{
    public long Uid { get; set; }
    public int Id { get; set; }
    public int Value { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
    public int Value4 { get; set; }
    public int Value5 { get; set; }
    public int Idx { get; set; }
}

public sealed class ProfileSupportPersonality
{
    public long Uid { get; set; }
    public int Idx { get; set; }
    public int Pid { get; set; }
    public int L { get; set; }
    public int Gl { get; set; }
    public int Gi { get; set; }
    public int Sid { get; set; }
}

public sealed class ProfileSupportEgo
{
    public long Uid { get; set; }
    public int Idx { get; set; }
    public int EgoIdx { get; set; }
    public int Id { get; set; }
    public int G { get; set; }
}

public sealed class RailwaySave
{
    public long Uid { get; set; }
    public int DungeonId { get; set; }
    public int PrevClearNode { get; set; } = -1;
    public int CurrentNode { get; set; }
    public int LastClearNode { get; set; }
    public int PayReward { get; set; }
    public int RewardState { get; set; }
    public int CurrentClearRotation { get; set; }
    public int LastEnterNodeId { get; set; } = 1;
    public int LastClearRotation { get; set; }
    public int InitSeed { get; set; }
    public int CurrentSeed { get; set; }
    public long FirstClearDate { get; set; }
}

public sealed class RailwaySaveUnit
{
    public long Uid { get; set; }
    public int DungeonId { get; set; }
    public int Pord { get; set; }
    public int Pid { get; set; }
    public int G { get; set; }
    public int L { get; set; }
    public int Sp { get; set; }
    public int Gi { get; set; }
    public int Sid { get; set; }
}

public sealed class RailwaySaveUnitEgo
{
    public long Uid { get; set; }
    public int DungeonId { get; set; }
    public int Pord { get; set; }
    public int Idx { get; set; }
    public int EgoId { get; set; }
    public int Gacksung { get; set; }
}
