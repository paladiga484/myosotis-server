using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetRailwayDungeonNodeAndLogAll")]
    public ActionResult<HttpResponseFormat<ResPacket_GetRailwayDungeonNodeAndLogAll>> GetRailwayDungeonNodeAndLogAll(
        [FromBody] HttpRequestFormat<ReqPacket_GetRailwayDungeonNodeAndLogAll> req)
    {
        return new HttpResponseFormat<ResPacket_GetRailwayDungeonNodeAndLogAll>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetRailwayDungeonNodeAndLogAll(),
            packetId = req.packetId,
        };
    }
}
