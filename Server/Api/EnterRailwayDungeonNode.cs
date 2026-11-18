using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("EnterRailwayDungeonNode")]
    public ActionResult<HttpResponseFormat<ResPacket_EnterRailwayDungeonNode>> EnterRailwayDungeonNode(
        [FromBody] HttpRequestFormat<ReqPacket_EnterRailwayDungeonNode> req)
    {
        var cur = req.parameters.nodeid;
        var prev = cur <= 1 ? cur : cur - 1;
        return new HttpResponseFormat<ResPacket_EnterRailwayDungeonNode>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterRailwayDungeonNode
            {
                nodeid = (uint)cur,
                currentNodeId = cur,
                prevClearNodeId = prev,
            },
            packetId = req.packetId,
        };
    }
}
