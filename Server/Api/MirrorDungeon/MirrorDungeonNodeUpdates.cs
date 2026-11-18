using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>Mid-node progress: choice events resolved, party and gifts as they stand.</summary>
    [HttpPost("UpdateMirrorDungeonMapNode")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateMirrorDungeonMapNode>>>
        UpdateMirrorDungeonMapNode(
            [FromBody] HttpRequestFormat<ReqPacket_UpdateMirrorDungeonMapNode> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var info = save.currentInfo;

        if (req.parameters.dungeonUnitList.Count > 0)
            info.dul = req.parameters.dungeonUnitList;

        foreach (var gift in req.parameters.updatedEgoGifts)
            if (gift.id != 0 && !info.spid.Contains(gift.id))
                info.spid.Add(gift.id);

        var choice = req.parameters.choiceEventData;
        if (choice.ri != 0 && !save.choiceEventList.Contains(choice.ri))
            save.choiceEventList.Add(choice.ri);

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_UpdateMirrorDungeonMapNode>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UpdateMirrorDungeonMapNode
            {
                prevChoiceEvent = [],
                currentEgoGifts = info.spid.Select(id => new DungeonMapEgoGiftFormat { id = id, un = 1 }).ToList(),
                dungeonUnitList = info.dul,
                cels = [],
            },
            packetId = req.packetId,
        };
    }

    /// <summary>Party state changed inside a node (healed, died, swapped).</summary>
    [HttpPost("UpdateMirrorDungeonUnits")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateMirrorDungeonUnits>>>
        UpdateMirrorDungeonUnits(
            [FromBody] HttpRequestFormat<ReqPacket_UpdateMirrorDungeonUnits> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        if (req.parameters.dungeonunitlist.Count > 0)
        {
            save.currentInfo.dul = req.parameters.dungeonunitlist;
            if (!await _mirrorSaves.StoreSaveAsync(uid, save))
                return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return new HttpResponseFormat<ResPacket_UpdateMirrorDungeonUnits>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UpdateMirrorDungeonUnits(),
            packetId = req.packetId,
        };
    }

    /// <summary>A battle that starts from an event choice rather than straight off the map.</summary>
    [HttpPost("EnterMirrorDungeonMapNodeBattleAfterChoice")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterMirrorDungeonMapNodeBattleAfterChoice>>>
        EnterMirrorDungeonMapNodeBattleAfterChoice(
            [FromBody] HttpRequestFormat<ReqPacket_EnterMirrorDungeonMapNodeBattleAfterChoice> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        if (req.parameters.dungeonUnitList.Count > 0)
        {
            save.currentInfo.dul = req.parameters.dungeonUnitList;
            if (!await _mirrorSaves.StoreSaveAsync(uid, save))
                return StatusCode(StatusCodes.Status500InternalServerError);
        }

        _logger.LogInformation(
            "MD post-choice battle for uid {Uid}: {Units} units, {Abnormalities} abnormalities",
            uid, req.parameters.participatedPids.Count, req.parameters.abnormalityids.Count);

        return new HttpResponseFormat<ResPacket_EnterMirrorDungeonMapNodeBattleAfterChoice>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterMirrorDungeonMapNodeBattleAfterChoice { abnormalityLogs = [] },
            packetId = req.packetId,
        };
    }
}
