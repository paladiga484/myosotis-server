using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    // TODO: maybe get this from static data?
    [HttpPost("GetAbnormalityLogData")]
    public ActionResult<HttpResponseFormat<ResPacket_GetAbnormalityLogData>> GetAbnormalityLogData(
        [FromBody] HttpRequestFormat<ReqPacket_GetAbnormalityLogData> req)
    {
        return new HttpResponseFormat<ResPacket_GetAbnormalityLogData>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetAbnormalityLogData(),
            packetId = req.packetId,
        };
    }
}
