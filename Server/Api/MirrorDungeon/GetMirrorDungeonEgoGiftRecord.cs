using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// The collection record — which E.G.O. gifts, theme floors and constraints the account has
    /// ever seen. The client asks for this right after entering, and getting an empty 404 back was
    /// what left the dungeon on a black screen.
    ///
    /// Nothing tracks these yet, so the record is empty. That is honest rather than harmful: an
    /// empty collection log is a valid state for an account that has not finished a run.
    /// </summary>
    [HttpPost("GetMirrorDungeonEgoGiftRecord")]
    public ActionResult<HttpResponseFormat<ResPacket_GetMirrorDungeonEgoGiftRecord>> GetMirrorDungeonEgoGiftRecord(
        [FromBody] HttpRequestFormat<ReqPacket_GetMirrorDungeonEgoGiftRecord> req)
    {
        return new HttpResponseFormat<ResPacket_GetMirrorDungeonEgoGiftRecord>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetMirrorDungeonEgoGiftRecord
            {
                acquiredegogifts = [],
                themeFloorIds = [],
                clearedConstraints = [],
            },
            packetId = req.packetId,
        };
    }
}
