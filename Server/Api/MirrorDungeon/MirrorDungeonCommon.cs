using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// First packet the client sends when the Mirror Dungeon menu opens.
    ///
    /// There is no <c>ResPacket_MirrorDungeonCommon</c> in the dumped types, so this replies with
    /// the envelope and no result body - <c>result</c> is serialised only when non-null. If the
    /// client turns out to want something here it will say so in myosotis.log, which is exactly
    /// what the spike is watching for.
    /// </summary>
    [HttpPost("MirrorDungeonCommon")]
    public ActionResult<HttpResponseFormat<ResPacket_GetStartBuffFInfoMirrorDungeon>> MirrorDungeonCommon(
        [FromBody] HttpRequestFormat<ReqPacket_MirrorDungeonCommon> req)
    {
        _logger.LogInformation("MirrorDungeonCommon: isOrigin={IsOrigin}", req.parameters.isOrigin);

        return new HttpResponseFormat<ResPacket_GetStartBuffFInfoMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            packetId = req.packetId,
        };
    }
}
