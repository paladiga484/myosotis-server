using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("UpdateLobbyCg")]
    public ActionResult<HttpResponseFormat<ResPacket_UpdateLobbyCg>> UpdateLobbyCg(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateLobbyCg> req)
    {
        return new HttpResponseFormat<ResPacket_UpdateLobbyCg>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                lobbyCG = req.parameters.lobbyCg,
            },
            result = new ResPacket_UpdateLobbyCg(),
            packetId = req.packetId,
        };
    }
}
