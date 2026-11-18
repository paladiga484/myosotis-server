using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// The client telling us its Mirror Dungeon state went wrong. It only carries a type code,
    /// but that code plus the saved run is the best evidence available when the dungeon dies, so
    /// this logs both rather than silently accepting it.
    ///
    /// Leaving it unimplemented was actively harmful: the client got an empty 404 while trying to
    /// report a fault, so the fault never surfaced anywhere.
    /// </summary>
    [HttpPost("SendMirrorDungeonLogError")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SendMirrorDungeonLogError>>> SendMirrorDungeonLogError(
        [FromBody] HttpRequestFormat<ReqPacket_SendMirrorDungeonLogError> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);

        if (save is null)
        {
            _logger.LogWarning(
                "CLIENT REPORTED MD ERROR type={Type} for uid {Uid} - and there is NO saved run",
                req.parameters.type, uid);
        }
        else
        {
            _logger.LogWarning(
                "CLIENT REPORTED MD ERROR type={Type} for uid {Uid}: dungeon {DungeonId}/{TypeIndex}, "
                + "{Nodes} nodes over {Floors} floors, eid={Eid}, units={Units}, themes={Themes}, "
                + "pools={Pools}, cleared={Cleared}",
                req.parameters.type, uid, save.dungeonId, save.idx,
                save.dungeonMap.ns.Count,
                save.dungeonMap.ns.Select(n => n.f).DefaultIfEmpty(-1).Max() + 1,
                save.currentInfo.eid, save.currentInfo.dul.Count,
                save.currentInfo.tfs.Count, save.currentInfo.seps.Count,
                save.currentInfo.cfs.Count);
        }

        return new HttpResponseFormat<ResPacket_SendMirrorDungeonLogError>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SendMirrorDungeonLogError(),
            packetId = req.packetId,
        };
    }
}
