using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("UpdateUserProfile")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateUserProfile>>> UpdateUserProfile(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateUserProfile> req)
    {
        var uid = HttpContext.GetUid();
        if (!await _profiles.UpdateAsync(
                uid,
                illustId: req.parameters.illustId,
                illustGacksungLevel: req.parameters.illustGacksungLevel,
                sentenceId: req.parameters.sentenceId,
                wordId: req.parameters.wordId,
                banners: req.parameters.banners,
                supports: req.parameters.supportPersonalities))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_UpdateUserProfile>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UpdateUserProfile(),
            packetId = req.packetId,
        };
    }
}
