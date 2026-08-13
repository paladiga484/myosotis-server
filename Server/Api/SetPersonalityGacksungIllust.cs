using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("SetPersonalityGacksungIllust")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SetPersonalityGacksungIllust>>> SetPersonalityGacksungIllust(
        [FromBody] HttpRequestFormat<ReqPacket_SetPersonalityGacksungIllust> req)
    {
        var uid = HttpContext.GetUid();
        if (!await _personalities.UpdateAsync(uid, req.parameters.personalityId, gacksungIllustType: req.parameters.type))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_SetPersonalityGacksungIllust>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                personalityList =
                [
                    await _personalities.GetAsync(uid, req.parameters.personalityId) ?? new PersonalityFormat(),
                ],
            },
            result = new ResPacket_SetPersonalityGacksungIllust(),
            packetId = req.packetId,
        };
    }
}
