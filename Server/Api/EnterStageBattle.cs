using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("EnterStageBattle")]
    public ActionResult<HttpResponseFormat<ResPacket_EnterStageBattle>> EnterStageBattle(
        [FromBody] HttpRequestFormat<ReqPacket_EnterStageBattle> req)
    {
        BlackForest.For(_config).NoteBattle(HttpContext.GetUid(), BlackForest.IsLocal(HttpContext));
        return new HttpResponseFormat<ResPacket_EnterStageBattle>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterStageBattle(),
            packetId = req.packetId,
        };
    }
}
