using Microsoft.AspNetCore.Mvc;
using Server.MirrorDungeon;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>Resume the run in progress.</summary>
    [HttpPost("ReEnterMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ReEnterMirrorDungeon>>> ReEnterMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_ReEnterMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
        {
            _logger.LogWarning("ReEnterMirrorDungeon with no run stored for uid {Uid}", uid);
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        _logger.LogInformation(
            "MD resumed by uid {Uid}: dungeon {DungeonId} type {TypeIndex}, {Nodes} nodes, {Cleared} floors cleared",
            uid, save.dungeonId, save.idx, save.dungeonMap.ns.Count, save.currentInfo.cfs.Count);

        return new HttpResponseFormat<ResPacket_ReEnterMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ReEnterMirrorDungeon { saveInfo = save },
            packetId = req.packetId,
        };
    }

    /// <summary>Party or E.G.O. changed mid-run.</summary>
    [HttpPost("UpdateMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateMirrorDungeon>>> UpdateMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        if (req.parameters.formation.Count > 0)
            MirrorDungeonState.ApplyFormation(save, req.parameters.formation);

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_UpdateMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UpdateMirrorDungeon { saveInfo = save },
            packetId = req.packetId,
        };
    }

    /// <summary>
    /// End the run, whether cleared or abandoned, and clear the save so the next entry starts a
    /// fresh one. A run counts as cleared once the final floor's boss is down.
    /// </summary>
    [HttpPost("ExitMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitMirrorDungeon>>> ExitMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_ExitMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);

        var dungeon = save is null ? null : _mirrorData.GetDungeon(save.dungeonId, save.idx);
        var lastFloor = dungeon is null
            ? 0
            : Math.Max(dungeon.floors.Count, dungeon.winFloorIdx + 1) - 1;

        var cleared = save is not null && save.currentInfo.cfs.Any(c => c.floor >= lastFloor);
        var statistics = save?.statistics ?? [];

        if (save is not null)
            await _mirrorSaves.ClearSaveAsync(uid);

        _logger.LogInformation(
            "MD run ended for uid {Uid}: cleared={Cleared} (floors done {Done}, last floor {Last})",
            uid, cleared, save?.currentInfo.cfs.Count ?? 0, lastFloor);

        return new HttpResponseFormat<ResPacket_ExitMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitMirrorDungeon
            {
                isEndDungeon = 1,
                isclear = cleared ? 1 : 0,
                statistics = statistics,
            },
            packetId = req.packetId,
        };
    }
}
