using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Iap;

public partial class IapController
{
    [HttpPost("UpdateSteamPendingPurchase")]
    public ActionResult<HttpResponseFormat<ResPacket_UpdateSteamPendingPurchase>> UpdateSteamPendingPurchase(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateSteamPendingPurchase> req)
    {
        return new HttpResponseFormat<ResPacket_UpdateSteamPendingPurchase>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UpdateSteamPendingPurchase(),
            packetId = req.packetId,
        };
    }
}
