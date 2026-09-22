using Microsoft.AspNetCore.Mvc;
using Resource;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Take the E.G.O. gifts offered after a node. These are recorded on the run, not just echoed:
    /// returning a well-formed empty response would let the client animate a reward the account
    /// never actually received.
    /// </summary>
    [HttpPost("AcquireRewardEgoGiftsMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_AcquireRewardEgoGiftsMirrorDungeon>>>
        AcquireRewardEgoGiftsMirrorDungeon(
            [FromBody] HttpRequestFormat<ReqPacket_AcquireRewardEgoGiftsMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var info = save.currentInfo;

        // Offers come from the floor's gift pool; selectIndexList indexes into it.
        var pool = OfferedGiftPool(save);
        var taken = new List<int>();
        foreach (var index in req.parameters.selectIndexList)
            if (index >= 0 && index < pool.Count)
                taken.Add(pool[index]);

        foreach (var giftId in taken)
            if (!info.spid.Contains(giftId))
                info.spid.Add(giftId);

        // The reward offer is consumed either way.
        info.rre.Clear();

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD gifts taken by uid {Uid}: {Taken} (now holding {Total})",
            uid, string.Join(", ", taken), info.spid.Count);

        return new HttpResponseFormat<ResPacket_AcquireRewardEgoGiftsMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_AcquireRewardEgoGiftsMirrorDungeon
            {
                egoGifts = info.spid.Select(id => new DungeonMapEgoGiftFormat { id = id, un = 1 }).ToList(),
                remainRewardEvent = info.rre,
                dungeonUnitList = info.dul,
                saveinfo = save,
            },
            packetId = req.packetId,
        };
    }

    /// <summary>Decline the offered gifts. The offer is still consumed.</summary>
    [HttpPost("RejectRewardEgoGiftsMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_RejectRewardEgoGiftsMirrorDungeon>>>
        RejectRewardEgoGiftsMirrorDungeon(
            [FromBody] HttpRequestFormat<ReqPacket_RejectRewardEgoGiftsMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        save.currentInfo.rre.Clear();
        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_RejectRewardEgoGiftsMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_RejectRewardEgoGiftsMirrorDungeon
            {
                remainRewardEvent = save.currentInfo.rre,
                saveinfo = save,
            },
            packetId = req.packetId,
        };
    }

    /// <summary>Post-battle reward pick.</summary>
    [HttpPost("AcquireMirrorDungeonBattleReward")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_AcquireMirrorDungeonBattleReward>>>
        AcquireMirrorDungeonBattleReward(
            [FromBody] HttpRequestFormat<ReqPacket_AcquireMirrorDungeonBattleReward> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var pool = OfferedGiftPool(save);
        foreach (var index in req.parameters.selectIndexList)
            if (index >= 0 && index < pool.Count && !save.currentInfo.spid.Contains(pool[index]))
                save.currentInfo.spid.Add(pool[index]);

        save.currentInfo.rre.Clear();

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD battle reward taken by uid {Uid}: indices {Idx}",
            uid, string.Join(", ", req.parameters.selectIndexList));

        return new HttpResponseFormat<ResPacket_AcquireMirrorDungeonBattleReward>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_AcquireMirrorDungeonBattleReward { saveinfo = save },
            packetId = req.packetId,
        };
    }

    /// <summary>What ending the run right now would pay, without ending it.</summary>
    [HttpPost("PreviewMirrorDungeonExitReward")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_PreviewMirrorDungeonExitReward>>>
        PreviewMirrorDungeonExitReward(
            [FromBody] HttpRequestFormat<ReqPacket_PreviewMirrorDungeonExitReward> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        var rewards = ExitRewardsFor(save);

        return new HttpResponseFormat<ResPacket_PreviewMirrorDungeonExitReward>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_PreviewMirrorDungeonExitReward
            {
                rewardList = rewards.Select(r => new MirrorDungeonExitRewardFormat
                {
                    chanceConsumption = r.consumption.bonusChanceConsumption,
                    moduleConsumption = r.consumption.moduleConsumption,
                    rewardList = r.rewardElements.Select(ToElement).ToList(),
                }).ToList(),
                totalConstraintScore = 0,
            },
            packetId = req.packetId,
        };
    }

    /// <summary>
    /// End the run and pay out. Items in the reward table are actually added to the inventory -
    /// anything that is not an item (EXP, battle pass points, banner records) is reported back but
    /// not credited, because there is nowhere in the schema to credit it yet.
    /// </summary>
    [HttpPost("AcquireMirrorDungeonExitReward")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_AcquireMirrorDungeonExitReward>>>
        AcquireMirrorDungeonExitReward(
            [FromBody] HttpRequestFormat<ReqPacket_AcquireMirrorDungeonExitReward> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        var rewards = ExitRewardsFor(save);
        var elements = rewards.SelectMany(r => r.rewardElements).ToList();

        var credited = 0;
        if (elements.Count > 0)
        {
            var held = await _items.ListAsync(uid);
            foreach (var group in elements.Where(e => e.type == "ITEM" && e.id > 0)
                         .GroupBy(e => e.id))
            {
                var current = held.FirstOrDefault(i => i.item_id == group.Key)?.num ?? 0;
                if (await _items.UpdateAsync(uid, group.Key, current + group.Sum(e => e.num)))
                    credited++;
            }
        }

        if (save is not null)
        {
            save.isEndDungeon = 1;
            await _mirrorSaves.ClearSaveAsync(uid);
        }

        _logger.LogInformation(
            "MD exit reward for uid {Uid}: {Rows} reward rows, {Elements} elements, {Credited} item stacks credited",
            uid, rewards.Count, elements.Count, credited);

        return new HttpResponseFormat<ResPacket_AcquireMirrorDungeonExitReward>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_AcquireMirrorDungeonExitReward
            {
                rewardList = elements.Select(ToElement).ToList(),
                saveInfo = save ?? new MirrorDungeonSaveInfoFormat { dungeonId = -1, idx = -1 },
                history = new MirrorDungeonHistoryFormat { dungeonid = save?.dungeonId ?? -1 },
                startBuffInfo = new MirrorDungeonStartBuffInfoFormat { dungeonid = save?.dungeonId ?? -1 },
            },
            packetId = req.packetId,
        };
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>The gift pool the current floor is offering from.</summary>
    private List<int> OfferedGiftPool(MirrorDungeonSaveInfoFormat save)
    {
        var floor = save.currentInfo.cfs.Count;
        var theme = save.currentInfo.tfs.FirstOrDefault(t => t.f == floor)
                    ?? save.currentInfo.tfs.FirstOrDefault();
        return theme is null ? [] : _mirrorData.GetThemeFloor(theme.tfid)?.egoGiftPool ?? [];
    }

    /// <summary>Reward rows the run has earned, by how many floors were cleared.</summary>
    private List<MdExitReward> ExitRewardsFor(MirrorDungeonSaveInfoFormat? save)
    {
        if (save is null)
            return [];

        var dungeon = _mirrorData.GetDungeon(save.dungeonId, save.idx);
        if (dungeon is null)
            return [];

        var highestCleared = save.currentInfo.cfs.Count == 0
            ? -1
            : save.currentInfo.cfs.Max(c => c.floor);

        return dungeon.rewardInfo.exitRewardListByCondition
            .Where(r => r.condition.clearedfloorIndex <= highestCleared)
            .ToList();
    }

    private static Element ToElement(MdRewardElement e) => new()
    {
        type = Enum.TryParse<ELEMENT_TYPE>(e.type, ignoreCase: true, out var parsed)
            ? parsed
            : ELEMENT_TYPE.NONE,
        _type = (int)(Enum.TryParse<ELEMENT_TYPE>(e.type, ignoreCase: true, out var p2)
            ? p2
            : ELEMENT_TYPE.NONE),
        id = e.id,
        num = e.num,
    };
}
