using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    // fuck off
    [HttpPost("EnterExpDungeonv2")]
    [HttpPost("EnterExpDungeon")]
    public ActionResult<HttpResponseFormat<ResPacket_EnterExpDungeon>> EnterExpDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_EnterExpDungeon> req)
    {
        return new HttpResponseFormat<ResPacket_EnterExpDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterExpDungeon(),
            packetId = req.packetId,
        };
    }
}
