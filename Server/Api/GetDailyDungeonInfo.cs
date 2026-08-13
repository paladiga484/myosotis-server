using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetDailyDungeonInfo")]
    public ActionResult<HttpResponseFormat<ResPacket_GetDailyDungeonInfo>> GetDailyDungeonInfo(
        [FromBody] HttpRequestFormat<ReqPacket_GetDailyDungeonInfo> req)
    {
        return new HttpResponseFormat<ResPacket_GetDailyDungeonInfo>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetDailyDungeonInfo(),
            packetId = req.packetId,
        };
    }
}
