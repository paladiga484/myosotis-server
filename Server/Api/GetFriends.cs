using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    // why?
    [HttpPost("GetFriendsData")]
    [HttpPost("GetFriends")]
    public ActionResult<HttpResponseFormat<ResPacket_GetFriends>> GetFriends(
        [FromBody] HttpRequestFormat<ReqPacket_GetFriends> req)
    {
        return new HttpResponseFormat<ResPacket_GetFriends>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetFriends(),
            packetId = req.packetId,
        };
    }
}
