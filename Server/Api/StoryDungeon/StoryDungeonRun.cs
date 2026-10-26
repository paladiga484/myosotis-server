using Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Resource;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetStoryDungeonSaveInfo")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetStoryDungeonSaveInfo>>> GetStoryDungeonSaveInfo(
        [FromBody] HttpRequestFormat<ReqPacket_GetStoryDungeonSaveInfo> req, [FromServices] StoryDungeonRepository saves)
    {
        var save = await saves.GetSaveAsync(HttpContext.GetUid());
        return StoryOk(new ResPacket_GetStoryDungeonSaveInfo { saveInfo = save ?? new StoryDungeonSaveInfoFormat() }, req.packetId);
    }

    /// <summary>A new run on the map of <c>stageid</c> (the dungeon id), starting at its start node
    /// with the team the client sends. A stage the static data has no map for is refused.</summary>
    [HttpPost("EnterStoryDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterStoryDungeon>>> EnterStoryDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_EnterStoryDungeon> req,
        [FromServices] StoryDungeonRepository saves, [FromServices] StoryDungeonData data)
    {
        var uid = HttpContext.GetUid();
        var p = req.parameters;
        var map = data.Map(p.stageid) ?? data.Map(p.nodeid);
        if (map is null)
        {
            _logger.LogWarning("EnterStoryDungeon uid={Uid}: no story dungeon map for stage {Stage} / node {Node}", uid, p.stageid, p.nodeid);
            return StatusCode(StatusCodes.Status400BadRequest);
        }
        var (floor, sector, node) = map.Start();
        var save = new StoryDungeonSaveInfoFormat
        {
            dungeonid = map.id,
            currentinfo = new StoryDungeonCurrentInfoFormat
            {
                dul = p.personalities,
                scpn = StoryNode(floor, sector, node),
                opn = [],
                scpegl = [],
                cu = false,
            },
        };
        if (!await saves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);
        _logger.LogInformation("EnterStoryDungeon uid={Uid}: dungeon {Dungeon}, {Units} units", uid, map.id, p.personalities.Count);
        return StoryOk(new ResPacket_EnterStoryDungeon { saveInfo = save, nodesRecord = await saves.GetRecordAsync(uid, map.id) }, req.packetId);
    }

    [HttpPost("ReEnterStoryDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ReEnterStoryDungeon>>> ReEnterStoryDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_ReEnterStoryDungeon> req, [FromServices] StoryDungeonRepository saves)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);
        return StoryOk(new ResPacket_ReEnterStoryDungeon
        {
            saveInfo = save,
            nodesRecord = await saves.GetRecordAsync(uid, save.dungeonid),
            isAllDie = 0,
        }, req.packetId);
    }

    /// <summary>Leaving the run: won if the map was cleared, no rewards yet; the save goes, the
    /// node record stays.</summary>
    [HttpPost("ExitStoryDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitStoryDungeon>>> ExitStoryDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_ExitStoryDungeon> req, [FromServices] StoryDungeonRepository saves)
    {
        var uid = HttpContext.GetUid();
        var save = await saves.GetSaveAsync(uid);
        var won = save?.currentinfo.cu ?? false;
        if (!await saves.ClearSaveAsync(uid))
            return StatusCode(StatusCodes.Status500InternalServerError);
        _logger.LogInformation("ExitStoryDungeon uid={Uid}: dungeon {Dungeon}, {Result}", uid, save?.dungeonid ?? 0, won ? "cleared" : "left");
        return StoryOk(new ResPacket_ExitStoryDungeon
        {
            saveInfo = new StoryDungeonSaveInfoFormat(),
            iswin = won,
            cleartype = won ? 1 : 0,
        }, req.packetId);
    }
}
