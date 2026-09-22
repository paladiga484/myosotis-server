using Microsoft.AspNetCore.Mvc;
using Server.MirrorDungeon;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>Step into a node. The client runs whatever happens inside it.</summary>
    [HttpPost("EnterMirrorDungeonMapNode")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterMirrorDungeonMapNode>>> EnterMirrorDungeonMapNode(
        [FromBody] HttpRequestFormat<ReqPacket_EnterMirrorDungeonMapNode> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var node = req.parameters.currentnode;
        save.currentInfo.eid = node.nid;

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD node entered by uid {Uid}: floor {Floor} step {Step} nid {Nid}",
            uid, node.f, node.s, node.nid);

        return new HttpResponseFormat<ResPacket_EnterMirrorDungeonMapNode>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterMirrorDungeonMapNode
            {
                currentNode = node,
                dungeonMap = save.dungeonMap,
                passingNodeIds = [],
                shopInfo = save.currentInfo.shop,
                egogifts = [],
                prevdul = save.currentInfo.prevdul,
                preves = save.currentInfo.preves,
                nr = (int)DUNGEON_NODERESULT.RUNNING,
                cels = [],
                cost = save.currentInfo.cost,
                changedHiddenNode = false,
                abnormalityLogs = [],
            },
            packetId = req.packetId,
        };
    }

    /// <summary>
    /// Leave a node and record what happened. The client sends the result and the state of the
    /// party; the server writes it down and notes a cleared floor when the boss falls.
    /// </summary>
    [HttpPost("ExitMirrorDungeonMapNode")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitMirrorDungeonMapNode>>> ExitMirrorDungeonMapNode(
        [FromBody] HttpRequestFormat<ReqPacket_ExitMirrorDungeonMapNode> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var node = req.parameters.currentnode;
        var result = (DUNGEON_NODERESULT)req.parameters.noderesult;
        var info = save.currentInfo;

        if (req.parameters.dungeonunitlist.Count > 0)
            info.dul = req.parameters.dungeonunitlist;

        // No longer standing in a node.
        info.eid = -1;

        if (result == DUNGEON_NODERESULT.WIN)
        {
            if (!info.preves.Contains(node.nid))
                info.preves.Add(node.nid);

            if (MirrorDungeonState.IsBossNode(save, node.nid))
            {
                var floor = MirrorDungeonState.FloorOf(save, node.nid);
                MirrorDungeonState.MarkFloorCleared(save, floor);
                _logger.LogInformation("MD floor {Floor} cleared by uid {Uid}", floor, uid);
            }
        }

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD node exited by uid {Uid}: nid {Nid} result {Result}", uid, node.nid, result);

        return new HttpResponseFormat<ResPacket_ExitMirrorDungeonMapNode>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitMirrorDungeonMapNode
            {
                currentInfo = info,
                abnormalityLogs = [],
            },
            packetId = req.packetId,
        };
    }
}
