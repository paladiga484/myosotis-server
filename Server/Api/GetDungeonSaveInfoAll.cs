using Common;
using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetDungeonSaveInfoAll")]
    public ActionResult<HttpResponseFormat<ResPacket_GetDungeonSaveInfoAll>> GetDungeonSaveInfoAll(
        [FromBody] HttpRequestFormat<ReqPacket_GetDungeonSaveInfoAll> req)
    {
        var railwayId = SeedValues.RailwayDungeonId;
        return new HttpResponseFormat<ResPacket_GetDungeonSaveInfoAll>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetDungeonSaveInfoAll
            {
                storySaveInfo = new StoryDungeonSaveInfoFormat(),
                mirrorOriginSaveInfo = new MirrorDungeonSaveInfoFormat
                {
                    dungeonId = -1,
                    idx = -1,
                    currentInfo = new MirrorDungeonCurrentInfoFormat { eid = -1 },
                },
                storyMirrorSaveInfo = new StoryMirrorDungeonSaveInfoFormat
                {
                    dungeonid = -1,
                    currentinfo = new StoryMirrorDungeonCurrentInfoFormat { eid = -1 },
                },
                mirrorDungeonClearInfos =
                [
                    new MirrorDungeonClearInfoFormat
                    {
                        dungeonid = railwayId,
                        idx = 0,
                        clearnumber = 1,
                        defeatnumber = 1,
                    },
                ],
                mirrorDungeonHistories =
                [
                    new MirrorDungeonHistoryFormat { dungeonid = railwayId },
                ],
            },
            packetId = req.packetId,
        };
    }
}
