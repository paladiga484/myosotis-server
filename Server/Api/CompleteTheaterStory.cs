using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("CompleteTheaterStory")]
    public ActionResult<HttpResponseFormat<ResPacket_CompleteTheaterStory>> CompleteTheaterStory(
        [FromBody] HttpRequestFormat<ReqPacket_CompleteTheaterStory> req)
    {
        var rewardId = req.parameters.storyId.Replace("P", "");
        return new HttpResponseFormat<ResPacket_CompleteTheaterStory>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_CompleteTheaterStory
            {
                isRewarded = true,
                theaterInfo = new UserTheaterInfoFormat
                {
                    rewardedIDList = [rewardId],
                },
            },
            packetId = req.packetId,
        };
    }
}
