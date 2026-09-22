using Common;
using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetDungeonSaveInfoAll")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetDungeonSaveInfoAll>>> GetDungeonSaveInfoAll(
        [FromBody] HttpRequestFormat<ReqPacket_GetDungeonSaveInfoAll> req)
    {
        var mirrorDungeonIds = _mirrorData.Dungeons.Keys.Select(k => k.Id).Distinct().OrderBy(id => id).ToList();

        // A real run if one is in progress, otherwise the "nothing here" shape the client expects.
        // Returning the stub unconditionally is what stopped it offering to resume.
        var uid = HttpContext.GetUid();
        var mirrorSave = await _mirrorSaves.GetSaveAsync(uid) ?? new MirrorDungeonSaveInfoFormat
        {
            dungeonId = -1,
            idx = -1,
            currentInfo = new MirrorDungeonCurrentInfoFormat { eid = -1 },
        };
        return new HttpResponseFormat<ResPacket_GetDungeonSaveInfoAll>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetDungeonSaveInfoAll
            {
                storySaveInfo = new StoryDungeonSaveInfoFormat(),
                mirrorOriginSaveInfo = mirrorSave,
                storyMirrorSaveInfo = new StoryMirrorDungeonSaveInfoFormat
                {
                    dungeonid = -1,
                    currentinfo = new StoryMirrorDungeonCurrentInfoFormat { eid = -1 },
                },
                // Clear counts and history per Mirror Dungeon difficulty. These used to be
                // reported against SeedValues.RailwayDungeonId (4), so the MD menu asked about
                // dungeon 7 and was handed records for a different dungeon entirely.
                mirrorDungeonClearInfos = mirrorDungeonIds
                    .SelectMany(id => _mirrorData.TypeIndexesFor(id)
                        .Select(typeIndex => new MirrorDungeonClearInfoFormat
                        {
                            dungeonid = id,
                            idx = typeIndex,
                            clearnumber = 0,
                            defeatnumber = 0,
                        }))
                    .ToList(),
                mirrorDungeonHistories = mirrorDungeonIds
                    .Select(id => new MirrorDungeonHistoryFormat { dungeonid = id })
                    .ToList(),
            },
            packetId = req.packetId,
        };
    }
}
