using System.Text.Json;
using System.Text.Json.Serialization;

// For types that the @-generation couldnt do

namespace Types.Server;

public enum ProjectGSGameState
{
    NONE = 0,
    BEFORE_LESSON = 1,
    IN_LESSON = 2,
    AFTER_LESSON = 3,
    END = 4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ELEMENT_TYPE
{
    NONE,
    ITEM,
    EXP,
    CHARACTER,
    PERSONALITY,
    EGO,
    STAMINA,
    BATTLEPASS_POINT,
    VENDING_MACHINE,
    ANNOUNCER,
    EGO_GIFT,
    GACHA,
    USERBANNER,
    VENDING_MACHINE_PERSONALITY,
    VENDING_MACHINE_CHARACTER,
    SEASONAL_R_BOX,
    SEASONAL_O_BOX,
    SEASONAL_PIECE,
    SEASONAL_GLOBAL_GROWTH_PIECE,
    EVENT_ITEM,
    USER_TICKET_DECO_LEFT,
    USER_TICKET_DECO_RIGHT,
    USER_TICKET_DECO_EGOBG,
    USER_TICKET_DECO_FOR_UI,
    MIRRORDUNGEON_COST,
    UNLOCK_CODE,
    CHANCE,
    USERBANNER_RECORD,
    PERSONALITY_SKIN,
    BGM,
    BGM_CHAPTER_CLEAR_RESULT
}

public class Element
{
    public ELEMENT_TYPE type { get; set; }
    public int id { get; set; }
    public int num { get; set; }
    public string[] tags { get; set; } = [];
}

public enum MISSION_CONDITION_TYPE
{
    NONE = 0,
    KILL_ENEMY_ONCE_BATTLE = 1,
    CLEAR_ALIVE_ALL = 2,
    CLEAR_EQUANNIMITY_ALL = 3,
    KILL_ENEMY_ONCE_ATTACK_SKILL = 4,
    TL_DAMAGE_ATTACK_SKILL = 5,
    MISSION_ALL_CLEAR = 6,
    KILL_ENEMY_COUNT_BY_ATTACK_SKILL = 7,
    CLEAR = 8,
    CLEAR_WITHIN_TURN = 9,
    CLEAR_WITHOUT_KILLED_ENEMY_BY_ENEMY = 10,
    ENEMY_KILLED_BY_ENEMY = 11,
    ENEMY_KILLED_BY_ENEMY_WITHIN_ONE_TURN = 12,
    ENEMY_NEGATIVE_SINBUFFED = 13,
    ENEMY_KILLED_BY_NEGAIVE_SINBUFF = 14,
    ENEMY_KILLED_BY_PLAYER_WITH_BUFF = 15,
    ENEMY_SKILL_NOTUSED = 16,
    ENEMY_DAMAGED_COUNT_BY_NEGATIVE_SINBUFF = 17,
    ENEMY_KILLED_BY_ONE_SKILL_HP_RATIO_80 = 18,
    ENEMY_NEGATIVE_BUFFED_ONE_TURN = 19,
    ENEMY_MULTIKILLED_ONE_TURN = 20,
    ENEMY_KILLED_BY_PARTICIPANT_ORDER = 21,
    ENEMY_KILLED_ALL = 22,
    CRITICAL_DMG_ONCE = 23,
    ENEMY_KILLED_HAS_BUFF_STACK = 24,
    CLEAR_WITHOUT_PLAYER_BUFF_STACK_REACHED = 25,
    ENEMY_MULTIKILLED_ONE_SKILL = 26,
    USE_PERSONALITY_ATKTYPE = 27,
    USE_PERSONALITY_SKILL = 28,
    USE_PERSONALITY_DEFENSE_SKILL = 29,
    USE_EGO_SKILL = 30,
    USE_FRIEND_SUPPORT = 31,
    CHECK_BATTLE_PASS = 32,
    CHECK_PASSIVE_SKILL = 33,
    ATTACK_DMG_PER_ONE_SKILL = 34,
    GIVE_BUFF_STACK_FOR_ENEMY = 35,
    GIVE_BUFF_STACK_FOR_ALLY = 36,
    KILL_ENEMY_NOT_LESS_HP_WITHIN_FIRST_TURN = 37,
    ERODE_AT_THE_SAME_TIME_FOR_ALLY = 38,
    ENEMY_NEGATIVE_BUFFED_ONE_TURN_FULL_VER = 39,
    TEAM_KILL_FOR_ALLY = 40,
    NO_TAKEN_DAMAGE_FOR_ALLY = 41,
    WALPU6_MISSION2 = 42,
    WALPU6_MISSION3 = 43,
    WALPU6_MISSION4 = 44,
    WALPU6_MISSION5 = 45,
    ALLY_USING_MINUS_COIN_SKILL = 46,
    GIVE_CHARGE_STACK_FOR_ALLY = 47,
    GIVE_BURST_SINKING_STACK_FOR_ALLY = 48,
    CLEAR_WITHOUT_PLAYER_BUFF_STACK_SUM_REACHED = 49,
    CRITICAL_PLAYER_NUMBER_FIRST_ROUND = 50,
    MAKE_PLAYER_PARRYING_POWER_MAXIMUM = 51,
    ON_ACTIVE_EGOGIFT_9154 = 52,
    MAX_RESONANCE_TYPE_IN_ROUND = 53,
    MAKE_PLAYER_SPEED_MAXIMUM = 54,
    KILL_ENEMY_COUNT_NOT_GREATER_THAN_ON_BATTLE = 55,
    WALPU8_MISSION_NOT_RETREAT = 56,
    WALPU8_MISSION_WIN_DUEL = 57,
    WALPU8_MISSION_GET_BUFF = 58,
    WALPU8_MISSION_GET_BUFF_ALL = 59,
    WALPU8_MISSION_NOT_USE_BUFF = 60,
    WALPU8_MISSION_KILL_ON_EQUIP_SKILL = 61,
    KILL_MIRROR_BOSS_ON_DEADLY_EDGE = 62,
    KILL_ENEMY_ALL_ON_ROUND = 63,
    PART_BROKEN_COUNT_ONCE_ATTACK_SKILL = 64,
    ENEMY_NEGATIVE_BUFF_MAX_COUNT = 65,
    ON_ACTIVE_EGOGIFT_9208 = 66,
    BRING_ME_ALCOHOL = 67,
    RING_FINGER_MAKING_ART = 68,
    STAGE_CLEAR = 69,
    ACQUIRE_STAGE_PROGRESS_REWARD = 70,
    PERSONALITY_LEVEL_UP = 71,
    PERSONALITY_GACKSUNG = 72,
    EGO_GACKSUNG = 73,
    USER_LEVEL_UP = 74,
    ACQUIRE_PERSONALITY = 75,
    ACQUIRE_EGO = 76,
    GACHA = 77,
    BATTLE_PASS_MISSION_CLEAR = 78,
    PURCHASE_ENKEPHALINE_MODULE = 79,
    CONSUME_ENKEPHALINE = 80,
    UPDATE_USER_PROFILE = 81,
    LUXCAVATION_DUNGEON_CLEAR = 82,
    LUXCAVATION_DUNGEON_SKIP = 83,
    EXP_DUNGEON_CLEAR = 84,
    EXP_DUNGEON_SKIP = 85,
    THREAD_DUNGEON_CLEAR = 86,
    THREAD_DUNGEON_SKIP = 87,
    MIRROR_DUNGEON_ENTER = 88,
    MIRROR_DUNGEON_CLEAR_NODE = 89,
    MIRROR_DUNGEON_CLEAR = 90,
    RAILWAY_DUNGEON_ENTER = 91,
    RAILWAY_DUNGEON_CLEAR_NODE = 92,
    RAILWAY_DUNGEON_CLEAR = 93,
    USE_ITEM = 94,
    UPDATE_FORMATION = 95,
    EXCHANGE_TWINE = 96,
    VENDING_MACHINE = 97,
    NEW_USER = 98,
    RETURN_USER = 99,
    BATTLE_PASS_LEVEL_UP = 100,
    MIRROR_DUNGEON_CLEAR_WITH_EGOGIFT = 101,
    MIRROR_DUNGEON_CLEAR_WITH_KEYWORD_EGOGIFT = 102,
    MIRROR_DUNGEON_CLEAR_WITH_TIER_EGOGIFT = 103,
    MIRROR_DUNGEON_CLEAR_WITH_COMBINE_EGOGIFT = 104,
    MIRROR_DUNGEON_EGOGIFT_COLLECTION = 105,
    MIRROR_DUNGEON_THEME_COLLECTION = 106,
    MIRROR_DUNGEON_FLOOR_CLEAR = 107,
    MIRROR_DUNGEON_CLEAR_WITH_SKILL_KEYWORD = 108,
    MIRROR_DUNGEON_CLEAR_WITH_UNIT_KEYWORD = 109,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_REFRESH = 110,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_BUY_EGOGIFT = 111,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_UPGRADE_EGOGIFT = 112,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_COST = 113,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_UPGRADE_EGOGIFT_MAX = 114,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_UPGRADE_SKILL = 115,
    MIRROR_DUNGEON_WIN_BATTLE_WITH_ONE_SURVIVOR = 116,
    MIRROR_DUNGEON_CLEAR_WITH_SPECIFIC_COMBINE_EGOGIFT = 117,
    MIRROR_DUNGEON_CLEAR_WITH_RAILWAY_THEME = 118,
    MIRROR_DUNGEON_CLEAR_WITH_STORY_THEME = 119,
    MIRROR_DUNGEON_CLEAR_WITH_UNIT_KEYWORD_T_YURODIVY_UNION = 120,
    THEME_CLEAR_WITH_PARTICIPATION = 121,
    MIRROR_DUNGEON_CLEAR_WITH_RENTAL_FORMATION = 122,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_UPGRADE_SKILL_MAX = 123,
    MIRROR_DUNGEON_CLEAR_WITH_SHOP_COMBINE = 124,
    MIRROR_DUNGEON_CLEAR_WITH_LAST_SHOP_COST_ZERO = 125,
    MIRROR_DUNGEON_CLEAR_WITH_ALL_LEVEL = 126,
    CULTIVATION_TEST_CLEAR = 127,
    CULTIVATION_ALL_CLASS_PASSED = 128,
    CULTIVATION_MAX_CONDITION_DAYS = 129,
    CULTIVATION_END = 130,
    CULTIVATION_CLASS_PASSED_OF = 131,
    CULTIVATION_STAT = 132,
    CULTIVATION_COMBO = 133,
    MIRROR_DUNGEON_CLEAR_WITH_CONSTRAINT = 134,
    MIRROR_DUNGEON_CLEAR_CONSTRAINT_TOTAL_SCORE = 135,
    MIRROR_DUNGEON_CLEAR_HIDDEN_BATTLE = 136,
    MIRROR_DUNGEON_ACQUIRE_SPECIFIC_EGOGIFT = 137,
    MIRROR_DUNGEON_CLEAR_WITHOUT_9083GIFT = 138,
    THEME_CLEAR_WITH_PARTICIPATION_SPECIAL_FOR_SPIDERS = 139,
    MIRROR_DUNGEON_CLEAR_DETECTING_SKILL = 140,
    THEME_CLEAR_WITH_PARTICIPATION_SPECIAL_FOR_BLOOD = 141,
    BOSS_RAID_CLEAR = 142,
}

public enum DANTE_ABILITY_CATEGORY
{
    DEFAULT = 0,
    RAILWAY_DUNGEON = 1,
    RAILWAY_DUNGEON_1001 = 2,
    BOSS_RAID = 3,
    BOSS_RAID_10002 = 4,
}

public enum RAILWAY_BATTLE_STATE_TYPE
{
    NONE = 0,
    MRR6_SPECIFIC = 1,
}

public enum MISSION_CATEGORY
{
    NONE = 0,
    EVENT_NEWYEAR_2024_NORMAL = 1,
    EVENT_NEWYEAR_2024_HARD = 2,
    EVENT_NEWYEAR_2024_COMMON = 3,
    EVENT_WALPU3_NORMAL = 4,
    EVENT_WALPU3_HARD = 5,
    EVENT_WALPU3_COMMON = 6,
    EVENT_WALPU4_NORMAL = 7,
    EVENT_WALPU4_HARD = 8,
    EVENT_WALPU4_COMMON = 9,
    EVENT_WALPU5_NORMAL = 10,
    EVENT_WALPU5_HARD = 11,
    EVENT_WALPU5_COMMON = 12,
    EVENT_WALPU6_NORMAL = 13,
    EVENT_WALPU6_HARD = 14,
    EVENT_WALPU6_COMMON = 15,
    EVENT_CULTIVATION = 16,
    EVENT_WALPU7 = 17,
    EVENT_WALPU8_NORMAL = 18,
    EVENT_WALPU8_HARD = 19,
    EVENT_WALPU8_COMMON = 20,
    EVENT_WALPU9 = 21,
    MIRROR_DUNGEON5_COLLECTION = 22,
    MIRROR_DUNGEON5_CLEAR = 23,
    MIRROR_DUNGEON5_DECK = 24,
    MIRROR_DUNGEON5_SHOP = 25,
    MIRROR_DUNGEON5_BATTLE = 26,
    MIRROR_DUNGEON5_ALL = 27,
    MIRROR_DUNGEON6_COLLECTION = 28,
    MIRROR_DUNGEON6_CLEAR = 29,
    MIRROR_DUNGEON6_DECK = 30,
    MIRROR_DUNGEON6_SHOP = 31,
    MIRROR_DUNGEON6_BATTLE = 32,
    MIRROR_DUNGEON6_EXTREME = 33,
    MIRROR_DUNGEON6_ALL = 34,
    MIRROR_DUNGEON6_HIDDEN = 35,
    MIRROR_DUNGEON7_COLLECTION = 36,
    MIRROR_DUNGEON7_CLEAR = 37,
    MIRROR_DUNGEON7_DECK = 38,
    MIRROR_DUNGEON7_SHOP = 39,
    MIRROR_DUNGEON7_BATTLE = 40,
    MIRROR_DUNGEON7_EXTREME = 41,
    MIRROR_DUNGEON7_ALL = 42,
    MIRROR_DUNGEON7_HIDDEN = 43,
    NEWBIE = 44,
    RETURN = 45,
    BOSS_RAID_01_NORMAL = 46,
    BOSS_RAID_01_HARD = 47,
    BOSS_RAID_02_NORMAL = 48,
    BOSS_RAID_02_HARD = 49,
    BOSS_RAID_03_NORMAL = 50,
    BOSS_RAID_03_HARD = 51,
}

public enum MISSION_STATE
{
    NONE = 0,
    OPENED = 1,
    ACHIEVED = 2,
    REWARDED = 3,
}

public class DateUtilJsonConverter : JsonConverter<DateUtil>
{
    public override DateUtil Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DateUtil(reader.GetDateTimeOffset());
    }

    public override void Write(Utf8JsonWriter writer, DateUtil value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

[JsonConverter(typeof(DateUtilJsonConverter))]
public readonly record struct DateUtil(DateTimeOffset Value)
{
    public static implicit operator DateTimeOffset(DateUtil d) => d.Value;
    public static implicit operator DateUtil(DateTimeOffset d) => new(d);
}

public class GachaLog
{
    public int gachaId { get; set; }
    public DateUtil gachaDate { get; set; } = new();
    public int paymentId { get; set; }
    public List<ItemFormat> payments { get; set; } = [];
    public List<GachaLogDetail> gachaLogDetails { get; set; } = [];
}

public class GachaLogDetail
{
    public ELEMENT_TYPE type { get; set; }
    public int id { get; set; }
    Element ex { get; set; } = new();
    Element _origin { get; set; } = new();
    Element sl { get; set; } = new();
}

public class PityPoint
{
    public int gachaID { get; set; }
    public int pityNumber { get; set; }
}

public enum ProjectGSLessonResult
{
    CRITICAL_SUCCESS = 0,
    SUCCESS = 1,
    NORMAL = 2,
    FAILURE = 3,
    CRITICAL_FAILURE = 4,
}

public class BattlePassMissionState
{
    public int id { get; set; }
    public int count { get; set; }
    public MISSION_STATE state { get; set; }
}

public class ServerErrorCodeStaticData
{
    public string errorMsgId { get; set; } = "";
    public List<int> errorCodeList { get; set; } = [];
    public bool isRetryable { get; set; }
}

public class UserStoryMirrorDungeonShopDataFormat
{
    public int ph { get; set; }
    public int pup { get; set; }
    public int upid { get; set; }
    public List<int> peg { get; set; } = [];
    public int pcf { get; set; }
    public List<int> egpool { get; set; } = [];
    public int rc { get; set; }
    public int fre { get; set; }
}

public class ServerDispatchInfo
{
    public string platform { get; set; } = "";
    public string serviceType { get; set; } = "";
    public string serverId { get; set; } = "";
    public string[] versions { get; set; } = [];
    public string serverUrl { get; set; } = "";
    public string logServerUrl { get; set; } = "";
    public string? presenceServerUrl { get; set; } = null;
    public string noticeUrl { get; set; } = "";
    public string cdnUrl { get; set; } = "";
    public int[] guestAuthErrors { get; set; } = [];
    public bool enablePacketCrypt { get; set; }
    public bool enableLogPacketCrypt { get; set; }
    public bool enableAgreementVersionCheck { get; set; }
    public bool enableNetworkingUIProcessPurchase { get; set; }
    public bool enableCheckUpdateCatalogSteamFixed { get; set; }
    public bool enableCheckUpdateCatalogToTitle { get; set; }
    public bool enableCheckUpdateTextAsset { get; set; }
    public bool enableCheckError { get; set; }
    public bool enableBLogSync { get; set; }
}
