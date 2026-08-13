using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    // notice stuff is moved to ../NoticeInfos.cs
    [HttpPost("FetchLatestSynchronousData")]
    public ActionResult<HttpResponseFormat<ResPacket_FetchLatestSynchronousData>> FetchLatestSynchronousData(
        [FromBody] HttpRequestFormat<ReqPacket_FetchLatestSynchronousData> req)
    {
        return new HttpResponseFormat<ResPacket_FetchLatestSynchronousData>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            synchronized = new SynchronizedFormat
            {
                version = req.userAuth.synchronousDataVersion + 17,
                noticeList = [],
                mailContentList = [],
            },
            packetId = req.packetId,
        };
    }
}
