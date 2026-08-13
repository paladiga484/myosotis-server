using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("UpdateAnnouncerPreset")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UpdateAnnouncerPreset>>> UpdateAnnouncerPreset(
        [FromBody] HttpRequestFormat<ReqPacket_UpdateAnnouncerPreset> req)
    {
        var uid = HttpContext.GetUid();
        if (!await _announcers.InsertPresetAsync(uid, req.parameters.presetId, req.parameters.presetAnnouncerIds) ||
            !await _announcers.SetCurrentPresetAsync(uid, req.parameters.presetId))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_UpdateAnnouncerPreset>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                announcer = await _announcers.GetAsync(uid),
            },
            result = new ResPacket_UpdateAnnouncerPreset(),
            packetId = req.packetId,
        };
    }
}
