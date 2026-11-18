using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

/// <summary>
/// Identity EXP tickets and level tickets, and buying Enkephalin Modules - by the game's own rules
/// (StaticDataService: the EXP table, the level cap and the module price from common-data, the
/// ticket values from the item data). Unhandled, both were empty 404s and the buttons did nothing.
/// </summary>
public partial class ApiController
{
    /// <summary>EXP tickets (PERSONALITY_EXP) and level tickets (PERSONALITY_LEVEL_TO) on one identity.</summary>
    [HttpPost("UsePersonalityExpItem")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UsePersonalityExpItem>>> UsePersonalityExpItem(
        [FromBody] HttpRequestFormat<ReqPacket_UsePersonalityExpItem> req)
    {
        var uid = HttpContext.GetUid();
        var p = req.parameters;
        var who = await _personalities.GetAsync(uid, p.targetPersonalityId);
        if (who is null || p.items.Count == 0)
            return StatusCode(StatusCodes.Status400BadRequest);

        // every item must be a growth item the player holds enough of, within the game's per-use limit
        var owned = (await _items.ListAsync(uid)).ToDictionary(i => i.item_id, i => i.num);
        var spend = p.items.GroupBy(i => i.item_id).ToDictionary(g => g.Key, g => g.Sum(i => i.num));
        if (spend.Values.Sum() > _staticData.MaxItemsOnceUsed
            || spend.Any(kv => kv.Value <= 0
                               || !_staticData.PersonalityGrowthItems.ContainsKey(kv.Key)
                               || owned.GetValueOrDefault(kv.Key) < kv.Value))
        {
            _logger.LogWarning("UsePersonalityExpItem uid {Uid} on {Pid}: refused ({Items})",
                uid, p.targetPersonalityId, string.Join(", ", spend.Select(kv => $"{kv.Key}x{kv.Value}")));
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var (level, exp) = (who.level, who.exp);
        foreach (var (itemId, num) in spend)
        {
            var (type, value) = _staticData.PersonalityGrowthItems[itemId];
            for (var n = 0; n < num; n++)
                (level, exp) = type == "PERSONALITY_LEVEL_TO"
                    ? Growth.LevelTo(level, exp, value, _staticData.PersonalityMaxLevel)
                    : Growth.AddExp(level, exp, value, _staticData.PersonalityExpTable, _staticData.PersonalityMaxLevel);
        }

        foreach (var (itemId, num) in spend)
            await _items.UpdateAsync(uid, itemId, owned[itemId] - num);
        await _personalities.UpdateAsync(uid, who.personality_id, level: level, exp: exp);
        var after = await _personalities.GetAsync(uid, who.personality_id) ?? who;
        _logger.LogInformation("UsePersonalityExpItem uid {Uid} on {Pid}: level {From} -> {To} (exp {Exp})",
            uid, who.personality_id, who.level, level, exp);

        return new HttpResponseFormat<ResPacket_UsePersonalityExpItem>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                personalityList = [after],
                itemList = spend.Select(kv => new ItemFormat { item_id = kv.Key, num = owned[kv.Key] - kv.Value }).ToList(),
            },
            result = new ResPacket_UsePersonalityExpItem { resultPersonality = after },
            packetId = req.packetId,
        };
    }

    /// <summary>Enkephalin into Enkephalin Modules, at the game's own price per module.</summary>
    [HttpPost("PurchaseEnkephalinModule")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_PurchaseEnkephalinModule>>> PurchaseEnkephalinModule(
        [FromBody] HttpRequestFormat<ReqPacket_PurchaseEnkephalinModule> req)
    {
        var uid = HttpContext.GetUid();
        var num = req.parameters.num;
        var user = await _users.GetAsync(uid);
        var cost = (long)num * _staticData.EnkephalinForAModule;
        if (user is null || num <= 0 || num > 9999 || user.stamina < cost)
        {
            _logger.LogWarning("PurchaseEnkephalinModule uid {Uid} x{Num}: refused (has {Stamina}, needs {Cost})",
                uid, num, user?.stamina ?? 0, cost);
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var modules = (await _items.ListAsync(uid)).FirstOrDefault(i => i.item_id == Growth.EnkephalinModule)?.num ?? 0;
        await _users.UpdateAsync(uid, stamina: user.stamina - (int)cost);
        await _items.UpdateAsync(uid, Growth.EnkephalinModule, modules + num);
        _logger.LogInformation("PurchaseEnkephalinModule uid {Uid}: {Num} module(s) for {Cost} Enkephalin", uid, num, cost);

        return new HttpResponseFormat<ResPacket_PurchaseEnkephalinModule>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                userInfo = await _users.GetAsync(uid),
                itemList = [new ItemFormat { item_id = Growth.EnkephalinModule, num = modules + num }],
            },
            result = new ResPacket_PurchaseEnkephalinModule(),
            packetId = req.packetId,
        };
    }
}

/// <summary>Identity levelling, kept apart so its arithmetic is easy to check.</summary>
public static class Growth
{
    /// <summary>The Enkephalin Module item (common item 11, sprite "enkephalinmodule").</summary>
    public const int EnkephalinModule = 11;

    /// <summary>
    /// Add EXP: exp is the progress inside the current level; table[n-1] is what level n needs to
    /// reach n+1. Levels rise while the EXP covers them; at the cap the extra EXP is dropped.
    /// </summary>
    public static (int Level, int Exp) AddExp(int level, int exp, int gain, IReadOnlyList<int> table, int cap)
    {
        if (level >= cap || table.Count == 0)
            return (Math.Min(level, Math.Max(cap, 1)), 0);
        long e = (long)exp + gain;
        while (level < cap && level - 1 < table.Count && e >= table[level - 1])
        {
            e -= table[level - 1];
            level++;
        }
        return level >= cap ? (cap, 0) : (level, (int)e);
    }

    /// <summary>A level ticket: up to that level (never down), the progress inside it reset.</summary>
    public static (int Level, int Exp) LevelTo(int level, int exp, int target, int cap)
    {
        var to = Math.Min(target, cap);
        return to > level ? (to, 0) : (level, exp);
    }
}
