using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("ClaimClosedGachaRewards")]
    public ActionResult<HttpResponseFormat<ResPacket_ClaimClosedGachaRewards>> ClaimClosedGachaRewards(
        [FromBody] HttpRequestFormat<ReqPacket_ClaimClosedGachaRewards> req)
    {
        return new HttpResponseFormat<ResPacket_ClaimClosedGachaRewards>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ClaimClosedGachaRewards(),
            packetId = req.packetId,
        };
    }
}
