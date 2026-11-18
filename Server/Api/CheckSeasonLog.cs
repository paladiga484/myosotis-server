using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("CheckSeasonLog")]
    public ActionResult<HttpResponseFormat<ResPacket_CheckSeasonLog>> CheckSeasonLog(
        [FromBody] HttpRequestFormat<ReqPacket_CheckSeasonLog> req)
    {
        return new HttpResponseFormat<ResPacket_CheckSeasonLog>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_CheckSeasonLog(),
            packetId = req.packetId,
        };
    }
}
