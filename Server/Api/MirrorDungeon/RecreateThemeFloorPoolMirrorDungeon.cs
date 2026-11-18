using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Re-roll the theme packs offered for the upcoming floor. Takes no parameters — the client
    /// just asks for a fresh set and gets the whole save back.
    /// </summary>
    [HttpPost("RecreateThemeFloorPoolMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_RecreateThemeFloorPoolMirrorDungeon>>>
        RecreateThemeFloorPoolMirrorDungeon(
            [FromBody] HttpRequestFormat<ReqPacket_RecreateThemeFloorPoolMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var floorCount = Math.Max(1, save.currentInfo.tfs.Count);
        var seed = unchecked(Environment.TickCount * 31 + (int)uid);

        save.currentInfo.tfps = _themePicker.PickPools(floorCount, seed);
        save.currentInfo.tfpsCreated = 1;

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD theme packs re-rolled for uid {Uid}: {Count} offers across {Floors} floors",
            uid, save.currentInfo.tfps.Count, floorCount);

        return new HttpResponseFormat<ResPacket_RecreateThemeFloorPoolMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_RecreateThemeFloorPoolMirrorDungeon { saveInfo = save },
            packetId = req.packetId,
        };
    }
}
