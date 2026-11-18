using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Starting-buff state for the dungeon's entry screen. Nothing is unlocked or enabled yet;
    /// buying and enabling start buffs is Phase B.
    /// </summary>
    [HttpPost("GetStartBuffFInfoMirrorDungeon")]
    public ActionResult<HttpResponseFormat<ResPacket_GetStartBuffFInfoMirrorDungeon>> GetStartBuffFInfoMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_GetStartBuffFInfoMirrorDungeon> req)
    {
        return new HttpResponseFormat<ResPacket_GetStartBuffFInfoMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetStartBuffFInfoMirrorDungeon
            {
                startBuffInfo = new MirrorDungeonStartBuffInfoFormat
                {
                    dungeonid = req.parameters.dungeonid,
                    bufstate = [],
                    enabled = [],
                },
            },
            packetId = req.packetId,
        };
    }
}
