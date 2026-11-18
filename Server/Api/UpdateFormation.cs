using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("UpdateFormation")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateFormation>>> UpdateFormation(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateFormation> req)
    {
        var uid = HttpContext.GetUid();
        if (!await _formations.UpdateAsync(uid, req.parameters.formation.id, req.parameters.formation))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_UpdateFormation>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                formationList = [req.parameters.formation],
            },
            result = new ResPacket_UpdateFormation(),
            packetId = req.packetId,
        };
    }
}
