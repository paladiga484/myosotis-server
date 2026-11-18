using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetProfileTicketDecoDatas")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetProfileTicketDecoDatas>>> GetProfileTicketDecoDatas(
        [FromBody] HttpRequestFormat<ReqPacket_GetProfileTicketDecoDatas> req)
    {
        var uid = HttpContext.GetUid();
        var tickets = await _tickets.GetAsync(uid);
        return new HttpResponseFormat<ResPacket_GetProfileTicketDecoDatas>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetProfileTicketDecoDatas
            {
                leftBorders = tickets.Left,
                rightBorders = tickets.Right,
                egoBackgrounds = tickets.EgoBg,
            },
            packetId = req.packetId,
        };
    }
}
