using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("LoadUserDataAll")]
    public ActionResult<HttpResponseFormat<ResPacket_LoadUserDataAll>> LoadUserDataAll(
        [FromBody] HttpRequestFormat<ReqPacket_LoadUserDataAll> req)
    {
        HttpResponseFormat<ResPacket_LoadUserDataAll> rsp = new()
        {
            serverInfo = new ServerInfo
            {
                version = "product"
            },
            state = "ok",
            packetId = req.packetId
        };

        return rsp;
    }
}

