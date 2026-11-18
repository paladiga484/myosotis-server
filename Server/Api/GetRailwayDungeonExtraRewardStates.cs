using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetRailwayDungeonExtraRewardStates")]
    public ActionResult<HttpResponseFormat<ResPacket_GetRailwayDungeonExtraRewardStates>> GetRailwayDungeonExtraRewardStates(
        [FromBody] HttpRequestFormat<ReqPacket_GetRailwayDungeonExtraRewardStates> req)
    {
        return new HttpResponseFormat<ResPacket_GetRailwayDungeonExtraRewardStates>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetRailwayDungeonExtraRewardStates
            {
                list = req.parameters.dungeonIds.Select(id => new RailwayExtraRewardStateByDungeonIdFormat
                {
                    dungeonId = id,
                    extraRewardState = [],
                }).ToList(),
            },
            packetId = req.packetId,
        };
    }
}
