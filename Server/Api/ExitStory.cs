using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("ExitStory")]
    public ActionResult<HttpResponseFormat<ResPacket_ExitStory>> ExitStory(
        [FromBody] HttpRequestFormat<ReqPacket_ExitStory> req)
    {
        return new HttpResponseFormat<ResPacket_ExitStory>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitStory(),
            packetId = req.packetId,
        };
    }
}
