using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("EnterRailwayDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterRailwayDungeon>>> EnterRailwayDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_EnterRailwayDungeon> req)
    {
        var uid = HttpContext.GetUid();
        if (!await _railway.UpdateSavePersonalitiesAsync(uid, req.parameters.dungeonId, req.parameters.personalities))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_EnterRailwayDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterRailwayDungeon
            {
                saveInfo = await _railway.GetSaveAsync(uid, req.parameters.dungeonId) ?? new RailwayDungeonSaveInfoFormat(),
                startNodeData = new RailwayNodeDataFormat(),
            },
            packetId = req.packetId,
        };
    }
}
