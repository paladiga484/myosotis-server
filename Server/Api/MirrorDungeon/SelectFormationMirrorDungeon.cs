using Microsoft.AspNetCore.Mvc;
using Server.MirrorDungeon;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Commit the party for the run. Until this lands the run has no units, which is why entering
    /// produced a map you could look at but not start.
    /// </summary>
    [HttpPost("SelectFormationMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SelectFormationMirrorDungeon>>> SelectFormationMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_SelectFormationMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        MirrorDungeonState.ApplyFormation(save, req.parameters.formation);

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD formation set for uid {Uid}: {Count} units", uid, req.parameters.formation.Count);

        return new HttpResponseFormat<ResPacket_SelectFormationMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SelectFormationMirrorDungeon { saveInfo = save },
            packetId = req.packetId,
        };
    }
}
