using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Login;

public partial class LoginController
{
    [HttpPost("CheckClientVersion")]
    public ActionResult<HttpResponseFormat<ResPacket_CheckClientVersion>> CheckClientVersion(
        [FromBody] HttpRequestFormat<ReqPacket_CheckClientVersion> req)
    {
        return new HttpResponseFormat<ResPacket_CheckClientVersion>()
        {
            serverInfo = new ServerInfo
            {
                version = "product"
            },
            state = "ok",
            result = new ResPacket_CheckClientVersion
            {
                timeoffset = 0
            },
            packetId = req.packetId
        };
    }
}

