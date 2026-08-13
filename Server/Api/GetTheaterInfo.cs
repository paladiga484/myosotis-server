using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("GetTheaterInfo")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_GetTheaterInfo>>> GetTheaterInfo(
        [FromBody] HttpRequestFormat<ReqPacket_GetTheaterInfo> req)
    {
        var uid = HttpContext.GetUid();
        var personalities = await _personalities.ListAsync(uid);
        return new HttpResponseFormat<ResPacket_GetTheaterInfo>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetTheaterInfo
            {
                theaterInfo = new UserTheaterInfoFormat
                {
                    rewardedIDList = personalities.Select(p => p.personality_id.ToString()).ToList(),
                },
            },
            packetId = req.packetId,
        };
    }
}
