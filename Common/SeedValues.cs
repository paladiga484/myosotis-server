namespace Common;

// default values for account
public static class SeedValues
{
    public const int UserLevel = 302;
    public const int UserStamina = 183; // enkephalin
    public const int PersonalityLevel = 60;
    public const int PersonalityGacksung = 4; // uptie
    public const int PersonalityGacksungIllustType = 1;
    public const int EgoGacksung = 4;
    public const int ItemCount = 10000;

    // hardcoded current railway season id eg. the 04 in railway-dungeon-04-a1c7p3.json
    public const int RailwayDungeonId = 4;

    // fallback season if cant derive from staticdata
    public const string BattlePassSeason = "7";

    public const int BattlePassLevel = 2026;
    public const int BattlePassExp = 2;
    public const int BattlePassTodayRandValue = 6;
    public const int BattlePassExRewardLevel = 2026;
    public const int BattlePassExRewardLimbusLevel = 2026;
    public const int BattlePassLimbusApplyLevel = 1;
    public const int BattlePassSpecialProductState = 2;

    // BATTLEPASS_REWARDSTATE.WITHLIMPASS
    public const int BattlePassRewardStateWithLimbus = 2;

    // this is kinda outdated as some banners can have more than two values now
    private static readonly Dictionary<int, (int Value, int Value2)> BannerValueMap = new()
    {
        [5] = (52, -1),
        [6] = (52, -1),
        [7] = (52, -1),
        [13] = (70, 5),
        [14] = (70, 5),
        [15] = (70, 5),
        [16] = (70, 5),
        [17] = (70, 5),
        [18] = (70, 5),
        [19] = (70, 5),
        [26] = (31, -1),
        [27] = (31, -1),
        [39] = (29, -1),
        [40] = (29, -1),
    };

    public static (int Value, int Value2) BannerValue(int bannerId) =>
        BannerValueMap.TryGetValue(bannerId, out var values) ? values : (-1, -1);
}
