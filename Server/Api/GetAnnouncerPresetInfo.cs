using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetAnnouncerPresetInfo")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetAnnouncerPresetInfo>>> GetAnnouncerPresetInfo(
        [FromBody] HttpRequestFormat<ReqPacket_GetAnnouncerPresetInfo> req)
    {
        var uid = HttpContext.GetUid();
        return new HttpResponseFormat<ResPacket_GetAnnouncerPresetInfo>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetAnnouncerPresetInfo
            {
                announcerInfo = await _announcers.GetAsync(uid),
            },
            packetId = req.packetId,
        };
    }
}
