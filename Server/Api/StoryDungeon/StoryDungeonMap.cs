using Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Resource;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    const uint NodeWin = 1, NodeDefeat = 2;           // Server.DUNGEON_NODERESULT

    [HttpPost("EnterStoryDungeonMapNode")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterStoryDungeonMapNode>>> EnterStoryDungeonMapNode(
        [FromBody] HttpRequestFormat<ReqPacket_EnterStoryDungeonMapNode> req, [FromServices] StoryDungeonRepository saves)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        var p = req.parameters;
        save.currentinfo.scpn = StoryNode(p.floornumber, p.sectornumber, p.nodeid);
        if (!await saves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);
        return StoryOk(new ResPacket_EnterStoryDungeonMapNode { node = save.currentinfo.scpn }, req.packetId);
    }

    /// <summary>A choice or event on the map: the team as it stands now and the gifts it picked
    /// up (a gift sent again replaces the one with its id).</summary>
    [HttpPost("UpdateStoryDungeonMapNode")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateStoryDungeonMapNode>>> UpdateStoryDungeonMapNode(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateStoryDungeonMapNode> req, [FromServices] StoryDungeonRepository saves)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        var cur = save.currentinfo;
        if (req.parameters.dungeonUnitList.Count > 0)
            cur.dul = req.parameters.dungeonUnitList;
        foreach (var gift in req.parameters.updatedEgoGifts)
        {
            cur.scpegl.RemoveAll(g => g.id == gift.id);
            cur.scpegl.Add(gift);
        }
        if (!await saves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);
        return StoryOk(new ResPacket_UpdateStoryDungeonMapNode { currentEgoGifts = cur.scpegl }, req.packetId);
    }

    [HttpPost("EnterStoryDungeonMapNodeBattleAfterChoice")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterStoryDungeonMapNodeBattleAfterChoice>>> EnterStoryDungeonMapNodeBattleAfterChoice(
        [FromBody] HttpRequestFormat<ReqPacket_EnterStoryDungeonMapNodeBattleAfterChoice> req, [FromServices] StoryDungeonRepository saves)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        if (req.parameters.dungeonUnitList.Count > 0)
        {
            save.currentinfo.dul = req.parameters.dungeonUnitList;
            if (!await saves.StoreSaveAsync(uid, save))
                return StatusCode(StatusCodes.Status500InternalServerError);
        }
        return StoryOk(new ResPacket_EnterStoryDungeonMapNodeBattleAfterChoice { isAllDie = 0 }, req.packetId);
    }

    /// <summary>A node ends. Won: it opens (and goes in the dungeon's record); won on the map's last
    /// node: the dungeon is cleared. Lost: everyone died - the run can still be left.</summary>
    [HttpPost("ExitStoryDungeonMapNode")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitStoryDungeonMapNode>>> ExitStoryDungeonMapNode(
        [FromBody] HttpRequestFormat<ReqPacket_ExitStoryDungeonMapNode> req,
        [FromServices] StoryDungeonRepository saves, [FromServices] StoryDungeonData data)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        var p = req.parameters;
        var cur = save.currentinfo;
        if (p.dungeonunitlist.Count > 0)
            cur.dul = p.dungeonunitlist;
        if (p.noderesult == NodeWin)
        {
            var node = p.openedNode != 0 ? p.openedNode : cur.scpn.nid;
            if (!cur.opn.Contains(node))
                cur.opn.Add(node);
            if (!await saves.AddRecordAsync(uid, save.dungeonid, node))
                return StatusCode(StatusCodes.Status500InternalServerError);
            if (data.Map(save.dungeonid)?.Final() == node)
                cur.cu = true;
        }
        if (!await saves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);
        return StoryOk(new ResPacket_ExitStoryDungeonMapNode
        {
            saveInfo = save,
            isAllDie = p.noderesult == NodeDefeat ? 1u : 0u,
        }, req.packetId);
    }

    [HttpPost("UpdateStoryDungeonUnits")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateStoryDungeonUnits>>> UpdateStoryDungeonUnits(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateStoryDungeonUnits> req, [FromServices] StoryDungeonRepository saves)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        save.currentinfo.dul = req.parameters.dungeonunitlist;
        if (!await saves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);
        return StoryOk(new ResPacket_UpdateStoryDungeonUnits(), req.packetId);
    }

    /// <summary>Back to the last save point the party opened (a "save" node), else the start.</summary>
    [HttpPost("ReturnSavePointStoryDungeonMap")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ReturnSavePointStoryDungeonMap>>> ReturnSavePointStoryDungeonMap(
        [FromBody] HttpRequestFormat<ReqPacket_ReturnSavePointStoryDungeonMap> req,
        [FromServices] StoryDungeonRepository saves, [FromServices] StoryDungeonData data)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        var map = save is null ? null : data.Map(save.dungeonid);
        if (save is null || map is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        var point = save.currentinfo.opn.LastOrDefault(n => map.Encounter(n) == "save");
        if (point != 0 && map.Where(point) is var (floor, sector))
            save.currentinfo.scpn = StoryNode(floor, sector, point);
        else
        {
            var start = map.Start();
            save.currentinfo.scpn = StoryNode(start.Floor, start.Sector, start.Node);
        }
        if (!await saves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);
        return StoryOk(new ResPacket_ReturnSavePointStoryDungeonMap { currentInfo = save.currentinfo }, req.packetId);
    }

    [HttpPost("GetStoryDungeonNodeRecord")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetStoryDungeonNodeRecord>>> GetStoryDungeonNodeRecord(
        [FromBody] HttpRequestFormat<ReqPacket_GetStoryDungeonNodeRecord> req, [FromServices] StoryDungeonRepository saves)
    {
        var nodes = await saves.GetRecordAsync(HttpContext.GetUid(), req.parameters.dungeonid);
        return StoryOk(new ResPacket_GetStoryDungeonNodeRecord { nodes = nodes }, req.packetId);
    }
}
