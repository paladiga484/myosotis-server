using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Take the starting E.G.O. gifts and build the per-floor gift pools for the run.
    /// </summary>
    [HttpPost("AcquireStartEgoGiftsAndCreateThemePoolMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_AcquireStartEgoGiftsAndCreateThemePoolMirrorDungeon>>>
        AcquireStartEgoGiftsAndCreateThemePoolMirrorDungeon(
            [FromBody] HttpRequestFormat<ReqPacket_AcquireStartEgoGiftsAndCreateThemePoolMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
            return StatusCode(StatusCodes.Status400BadRequest);

        var info = save.currentInfo;
        info.sepsId = req.parameters.selectedSetId;
        info.sepsCreated = 1;

        // One pool per chosen theme floor, drawn from that theme's gift pool.
        info.seps = save.currentInfo.tfs
            .Select(t => new MirrorDungeonEgoGiftPoolSetFormat
            {
                setId = t.tfid,
                keyword = "",
                pool = _mirrorData.GetThemeFloor(t.tfid)?.egoGiftPool ?? [],
            })
            .ToList();

        foreach (var giftId in req.parameters.selectedEgoGiftIds)
            if (!info.spid.Contains(giftId))
                info.spid.Add(giftId);

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "MD start gifts for uid {Uid}: set {Set}, {Gifts} gifts, {Pools} pools",
            uid, info.sepsId, req.parameters.selectedEgoGiftIds.Count, info.seps.Count);

        return new HttpResponseFormat<ResPacket_AcquireStartEgoGiftsAndCreateThemePoolMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_AcquireStartEgoGiftsAndCreateThemePoolMirrorDungeon { saveInfo = save },
            packetId = req.packetId,
        };
    }
}
