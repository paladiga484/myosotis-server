using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    // omfg i hate project moon
    [HttpPost("GetUserCouponState")]
    [HttpPost("GetCouponFailState")]
    public ActionResult<HttpResponseFormat<ResPacket_GetCouponFailState>> GetCouponFailState(
        [FromBody] HttpRequestFormat<ReqPacket_GetCouponFailState> req)
    {
        return new HttpResponseFormat<ResPacket_GetCouponFailState>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetCouponFailState
            {
                ispossiblestate = true,
            },
            packetId = req.packetId,
        };
    }
}
