using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Log;

public partial class LogController
{
    [HttpPost("GetMailLogAll")]
    public ActionResult<HttpResponseFormat<ResPacket_GetMailLogAll>> GetMailLogAll(
        [FromBody] HttpRequestFormat<ReqPacket_GetMailLogAll> req)
    {
        return new HttpResponseFormat<ResPacket_GetMailLogAll>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetMailLogAll(),
            packetId = req.packetId,
        };
    }
}
