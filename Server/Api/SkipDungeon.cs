using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

/// <summary>
/// Luxcavation "Skip" (EXP and Thread). Unhandled, these were empty 404s, which the client
/// cannot deserialise, so the skip button threw and the screen hung. Stamina is not charged
/// and no drops are granted: the account is seeded with what those runs would give.
/// </summary>
public partial class ApiController
{
    [HttpPost("SkipExpDungeonv2")]
    [HttpPost("SkipExpDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SkipExpDungeon>>> SkipExpDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_SkipExpDungeon> req)
    {
        var user = await _users.GetAsync(HttpContext.GetUid());
        _logger.LogInformation("SkipExpDungeon {Dungeon} x{Count}: no drops granted",
            req.parameters.dungeonid, req.parameters.progressCount);

        return new HttpResponseFormat<ResPacket_SkipExpDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SkipExpDungeon { userExp = user?.exp ?? 0 },
            packetId = req.packetId,
        };
    }

    [HttpPost("SkipThreadDungeonv2")]
    [HttpPost("SkipThreadDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SkipThreadDungeon>>> SkipThreadDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_SkipThreadDungeon> req)
    {
        var user = await _users.GetAsync(HttpContext.GetUid());
        _logger.LogInformation("SkipThreadDungeon {Dungeon} lvl {Level} x{Count}: no drops granted",
            req.parameters.dungeonid, req.parameters.dungeonlevel, req.parameters.progressCount);

        return new HttpResponseFormat<ResPacket_SkipThreadDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SkipThreadDungeon { userExp = user?.exp ?? 0 },
            packetId = req.packetId,
        };
    }
}
