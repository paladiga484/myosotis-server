using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetDanteNoteState")]
    public ActionResult<HttpResponseFormat<ResPacket_GetDanteNoteState>> GetDanteNoteState(
        [FromBody] HttpRequestFormat<ReqPacket_GetDanteNoteState> req)
    {
        return new HttpResponseFormat<ResPacket_GetDanteNoteState>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetDanteNoteState
            {
                page = 49,
                todayPage = 65,
            },
            packetId = req.packetId,
        };
    }
}
