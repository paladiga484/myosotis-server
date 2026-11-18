using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("UpdateProfileTicketDeco")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateProfileTicketDeco>>> UpdateProfileTicketDeco(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateProfileTicketDeco> req)
    {
        var uid = HttpContext.GetUid();
        if (!await _profiles.UpdateAsync(
                uid,
                leftBorderId: req.parameters.leftBorderId,
                rightBorderId: req.parameters.rightBorderId,
                egoBackgroundId: req.parameters.egoBackgroundId))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_UpdateProfileTicketDeco>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UpdateProfileTicketDeco
            {
                leftBorderId = req.parameters.leftBorderId,
                rightBorderId = req.parameters.rightBorderId,
                egoBackgroundId = req.parameters.egoBackgroundId,
            },
            packetId = req.packetId,
        };
    }
}
