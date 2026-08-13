using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetUserBanners")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetUserBanners>>> GetUserBanners(
        [FromBody] HttpRequestFormat<ReqPacket_GetUserBanners> req)
    {
        var uid = HttpContext.GetUid();
        return new HttpResponseFormat<ResPacket_GetUserBanners>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetUserBanners
            {
                banners = await _banners.ListAsync(uid),
            },
            packetId = req.packetId,
        };
    }
}
