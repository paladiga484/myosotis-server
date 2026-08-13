using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetBossRaidStates")]
    public ActionResult<HttpResponseFormat<ResPacket_GetBossRaidStates>> GetBossRaidStates(
        [FromBody] HttpRequestFormat<ReqPacket_GetBossRaidStates> req)
    {
        return new HttpResponseFormat<ResPacket_GetBossRaidStates>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetBossRaidStates(),
            packetId = req.packetId,
        };
    }
}
