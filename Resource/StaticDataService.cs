using System.Text.Json;
using Common;
using Microsoft.Extensions.Logging;
using Types.Server;

namespace Resource;

public sealed class StaticDataService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private readonly string _root;
    private readonly ILogger<StaticDataService> _logger;

    private IReadOnlyList<int> _personalityIds = [];
    private IReadOnlyList<int> _egoIds = [];
    private IReadOnlyList<int> _itemIds = [];
    private IReadOnlyList<int> _announcerIds = [];
    private IReadOnlyList<int> _userBannerIds = [];
    private IReadOnlyList<int> _borderRightIds = [];
    private IReadOnlyList<int> _borderLeftIds = [];
    private IReadOnlyList<int> _egoBgIds = [];
    private IReadOnlyList<int> _unlockCodeIds = [];
    private IReadOnlyList<int> _nodeIds = [];
    private IReadOnlyList<(int Id, string ProductId)> _iapProducts = [];
    private IReadOnlyList<int> _danteAbilityIds = [];
    private Dictionary<int, IReadOnlyList<GachaEntry>> _gacha = [];

    public StaticDataService(string staticDataRoot, ILogger<StaticDataService> logger)
    {
        _root = staticDataRoot;
        _logger = logger;
    }

    public IReadOnlyList<int> PersonalityIds => _personalityIds;
    public IReadOnlyList<int> EgoIds => _egoIds;
    public IReadOnlyList<int> ItemIds => _itemIds;
    public IReadOnlyList<int> AnnouncerIds => _announcerIds;
    public IReadOnlyList<int> UserBannerIds => _userBannerIds;
    public IReadOnlyList<int> BorderRightIds => _borderRightIds;
    public IReadOnlyList<int> BorderLeftIds => _borderLeftIds;
    public IReadOnlyList<int> EgoBgIds => _egoBgIds;
    public IReadOnlyList<int> UnlockCodeIds => _unlockCodeIds;
    public IReadOnlyList<int> NodeIds => _nodeIds;
    public IReadOnlyList<(int Id, string ProductId)> IapProducts => _iapProducts;
    public IReadOnlyList<int> DanteAbilityIds => _danteAbilityIds;

    public IReadOnlyList<ChanceFormat> Chances { get; private set; } = [];
    public BattlePassFormat? BattlePass { get; private set; }
    public IReadOnlyList<MainChapterStateFormat> MainChapterStates { get; private set; } = [];
    public IReadOnlyList<UnlockCodeFormat> UnlockCodes { get; private set; } = [];
    public IReadOnlyList<MembershipFormat> Memberships { get; private set; } = [];

    public IReadOnlyList<GachaEntry> GetGacha(int id) =>
        _gacha.TryGetValue(id, out var entries) ? entries : [];

    public async Task LoadAllAsync()
    {
        _personalityIds = await LoadIdsAsync("personality");
        _egoIds = await LoadIdsAsync("ego");
        _itemIds = await LoadIdsAsync("item");
        _announcerIds = await LoadIdsAsync("announcer");
        _userBannerIds = await LoadIdsAsync("userbanner");
        _borderRightIds = await LoadTicketIdsAsync("userticket-r");
        _borderLeftIds = await LoadTicketIdsAsync("userticket-l");
        _egoBgIds = await LoadTicketIdsAsync("userticket-egobg");
        _unlockCodeIds = await LoadUnlockCodeIdsAsync();
        _nodeIds = await LoadNodeIdsAsync();
        _iapProducts = await LoadIapProductsAsync();
        _danteAbilityIds = await LoadDanteAbilityIdsAsync();

        Chances = await LoadChancesAsync();
        BattlePass = await LoadBattlePassAsync();
        UnlockCodes = _unlockCodeIds.Select(id => new UnlockCodeFormat { unlockcode = id }).ToList();
        MainChapterStates = BuildMainChapterStates(_unlockCodeIds);
        Memberships = BuildMemberships(_iapProducts);
        _gacha = await LoadGachaAsync();

        _logger.LogInformation(
            "StaticData loaded: {Personalities} personalities, {Egos} egos, {Items} items, {Announcers} announcers, {Banners} banners, {UnlockCodes} unlock codes, {Chances} chances, {Gachas} gacha files",
            _personalityIds.Count, _egoIds.Count, _itemIds.Count, _announcerIds.Count,
            _userBannerIds.Count, _unlockCodeIds.Count, Chances.Count, _gacha.Count);
    }

    private async Task<IReadOnlyList<int>> LoadIdsAsync(string folder)
    {
        var ids = new HashSet<int>();
        foreach (var file in EnumerateJson(folder))
        {
            var data = await DeserializeAsync<IdListFile>(file);
            if (data is null)
                continue;
            foreach (var item in data.list)
                ids.Add(item.id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<IReadOnlyList<int>> LoadTicketIdsAsync(string folder)
    {
        var ids = new HashSet<int>();
        foreach (var file in EnumerateJson(folder))
        {
            var data = await DeserializeAsync<IdListFile>(file);
            if (data is null)
                continue;
            foreach (var item in data.list.Where(i => i.id > 7))
                ids.Add(item.id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<IReadOnlyList<int>> LoadUnlockCodeIdsAsync()
    {
        var ids = new HashSet<int>();
        foreach (var file in EnumerateJson("unlockcode"))
        {
            if (file.Contains("event", StringComparison.OrdinalIgnoreCase))
                continue;
            var data = await DeserializeAsync<IdListFile>(file);
            if (data is null)
                continue;
            foreach (var item in data.list.Where(i => i.id != 107))
                ids.Add(item.id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<IReadOnlyList<int>> LoadNodeIdsAsync()
    {
        var ids = new HashSet<int>();
        foreach (var file in EnumerateJson("stagenodereward"))
        {
            var data = await DeserializeAsync<NodeListFile>(file);
            if (data is null)
                continue;
            foreach (var item in data.list)
                ids.Add(item.nodeid);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<IReadOnlyList<ChanceFormat>> LoadChancesAsync()
    {
        var seen = new HashSet<int>();
        var chances = new List<ChanceFormat>();
        foreach (var file in EnumerateJson("chance"))
        {
            if (Path.GetFileNameWithoutExtension(file).Contains("gacha", StringComparison.OrdinalIgnoreCase))
                continue;
            var data = await DeserializeAsync<ChanceListFile>(file);
            if (data is null)
                continue;
            foreach (var item in data.list.Where(c => seen.Add(c.id)))
                chances.Add(new ChanceFormat { id = item.id, value = item.max });
        }

        return chances;
    }

    private async Task<BattlePassFormat?> LoadBattlePassAsync()
    {
        var files = EnumerateJson("battlepass").ToList();
        BattlePassItem? item = null;
        foreach (var file in files)
        {
            var data = await DeserializeAsync<BattlePassListFile>(file);
            var current = data?.list.FirstOrDefault(x => x.isCurrentForUI);
            if (current is not null)
            {
                item = current;
                break;
            }
        }

        if (item is null)
        {
            foreach (var file in files)
            {
                var parts = Path.GetFileNameWithoutExtension(file).Split('-');
                if (parts.Length < 2 || parts[1] != SeedValues.BattlePassSeason)
                    continue;

                var data = await DeserializeAsync<BattlePassListFile>(file);
                item = data?.list.FirstOrDefault();
                if (item is not null)
                    break;
            }
        }

        if (item is not null)
        {
            var missions = item.passMissionsInfo.missionList
                .Select(m => new BattlePassMissionState
                {
                    id = m.missionId,
                    count = m.countMax,
                    state = MISSION_STATE.OPENED,
                })
                .ToList();

            return new BattlePassFormat
            {
                is_limbus = true,
                level = SeedValues.BattlePassLevel,
                exp = SeedValues.BattlePassExp,
                today_rand_value = SeedValues.BattlePassTodayRandValue,
                ex_reward_level = SeedValues.BattlePassExRewardLevel,
                ex_reward_limbus_level = SeedValues.BattlePassExRewardLimbusLevel,
                limbus_apply_level = SeedValues.BattlePassLimbusApplyLevel,
                special_product_state = SeedValues.BattlePassSpecialProductState,
                rewards_state = Enumerable.Repeat(SeedValues.BattlePassRewardStateWithLimbus, item.passRewardsInfo.maxLevel).ToList(),
                missions_state = missions,
            };
        }

        _logger.LogWarning(
            "battlepass static data not found (no isCurrentForUI entry, season {Season} filename missing); BattlePass defaults to empty.",
            SeedValues.BattlePassSeason);
        return null;
    }

    private static IReadOnlyList<MainChapterStateFormat> BuildMainChapterStates(IReadOnlyList<int> unlockCodeIds)
    {
        var mainChapters = new List<MainChapterStateFormat>();
        foreach (var chapter in new[] { 1, 91 })
        {
            var subChapters = new List<SubChapterStateFormat>();
            foreach (var code in unlockCodeIds.Where(code => code / 100 == chapter).OrderBy(code => code))
            {
                var nodes = new List<NodeStateFormat>();
                for (int sub = 1; sub <= 99; sub++)
                    nodes.Add(new NodeStateFormat { id = code * 100 + sub, ct = 2, cn = 1, dn = 0 });

                subChapters.Add(new SubChapterStateFormat { id = code, nss = nodes, rss = [1, 2, 3, 10] });
            }

            mainChapters.Add(new MainChapterStateFormat { id = chapter, subcss = subChapters });
        }

        return mainChapters;
    }

    private async Task<IReadOnlyList<(int Id, string ProductId)>> LoadIapProductsAsync()
    {
        var products = new List<(int Id, string ProductId)>();
        foreach (var file in EnumerateJson("iap-product"))
        {
            if (Path.GetFileNameWithoutExtension(file).Contains("a1", StringComparison.OrdinalIgnoreCase))
                continue;
            var data = await DeserializeAsync<IapProductListFile>(file);
            if (data is null)
                continue;
            products.AddRange(data.list.Select(i => (i.id, i.productId)));
        }

        return products;
    }

    private static IReadOnlyList<MembershipFormat> BuildMemberships(IReadOnlyList<(int Id, string ProductId)> products)
    {
        return products
            .Where(p => p.ProductId.Length > 0)
            .Where(p =>
            {
                var lower = p.ProductId.ToLowerInvariant();
                return (lower.Contains("monthly") || lower.Contains("pass")) && !lower.Contains("fixed");
            })
            .Select(p => new MembershipFormat { iap_id = p.Id, expiry_date = "2098-11-09T22:10:00.864Z" })
            .ToList();
    }

    private async Task<IReadOnlyList<int>> LoadDanteAbilityIdsAsync()
    {
        var ids = new HashSet<int>();
        foreach (var file in EnumerateJson("dante-ability"))
        {
            var data = await DeserializeAsync<IdListFile>(file);
            if (data is null)
                continue;
            foreach (var item in data.list)
                ids.Add(item.id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<Dictionary<int, IReadOnlyList<GachaEntry>>> LoadGachaAsync()
    {
        var result = new Dictionary<int, IReadOnlyList<GachaEntry>>();
        foreach (var file in EnumerateJson("gacha"))
        {
            var parts = Path.GetFileNameWithoutExtension(file).Split('-');
            if (parts.Length < 2 || !int.TryParse(parts[1], out var id))
                continue;

            var data = await DeserializeAsync<GachaListFile>(file);
            if (data is null)
                continue;

            result[id] = data.list.Select(g => new GachaEntry(
                g.payments.Select(p => new GachaPayment(p.paymentId, p.count)).ToList(),
                g.contents.Select(c => new GachaContent(c.groupType, c.elementType, c.elementIdList)).ToList()))
                .ToList();
        }

        return result;
    }

    private IReadOnlyList<string> EnumerateJson(string folder)
    {
        var full = Path.Combine(_root, folder);
        if (!Directory.Exists(full))
        {
            _logger.LogWarning("Static data folder missing: {Folder}", full);
            return [];
        }

        return Directory.EnumerateFiles(full, "*.json", SearchOption.AllDirectories).ToList();
    }

    private async Task<T?> DeserializeAsync<T>(string path) where T : class
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse static file {Path}", path);
            return null;
        }
    }

    private sealed class IdListFile { public List<IdItem> list { get; set; } = []; }
    private sealed class IdItem { public int id { get; set; } }
    private sealed class NodeListFile { public List<NodeItem> list { get; set; } = []; }
    private sealed class NodeItem { public int nodeid { get; set; } }
    private sealed class ChanceListFile { public List<ChanceItem> list { get; set; } = []; }
    private sealed class ChanceItem { public int id { get; set; } public int max { get; set; } }
    private sealed class BattlePassListFile { public List<BattlePassItem> list { get; set; } = []; }
    private sealed class BattlePassItem
    {
        public int id { get; set; }
        public bool isCurrentForUI { get; set; }
        public PassRewardsInfo passRewardsInfo { get; set; } = new();
        public PassMissionsInfo passMissionsInfo { get; set; } = new();
    }
    private sealed class PassRewardsInfo { public int maxLevel { get; set; } }
    private sealed class PassMissionsInfo { public List<PassMission> missionList { get; set; } = []; }
    private sealed class PassMission { public int missionId { get; set; } public int countMax { get; set; } }
    private sealed class IapProductListFile { public List<IapProductItem> list { get; set; } = []; }
    private sealed class IapProductItem { public int id { get; set; } public string productId { get; set; } = ""; }
    private sealed class GachaListFile { public List<GachaItem> list { get; set; } = []; }
    private sealed class GachaItem
    {
        public List<GachaPaymentItem> payments { get; set; } = [];
        public List<GachaContentItem> contents { get; set; } = [];
    }
    private sealed class GachaPaymentItem { public int paymentId { get; set; } public int count { get; set; } }
    private sealed class GachaContentItem
    {
        public string groupType { get; set; } = "";
        public string elementType { get; set; } = "";
        public List<int> elementIdList { get; set; } = [];
    }
}

public sealed record GachaEntry(
    IReadOnlyList<GachaPayment> Payments,
    IReadOnlyList<GachaContent> Contents);

public sealed record GachaPayment(int PaymentId, int Count);

public sealed record GachaContent(string GroupType, string ElementType, IReadOnlyList<int> ElementIdList);
