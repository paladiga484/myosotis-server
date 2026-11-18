using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// The shop cluster, answered coherently as "open but empty" rather than left to 404.
    ///
    /// Buying, selling, crafting and healing are not implemented. The difference this makes is
    /// that the client gets a well-formed empty shop and moves on, instead of deserialising an
    /// empty 404 body and throwing - which is what put the run on a black screen. Nothing here
    /// pretends a purchase succeeded: the save is returned unchanged, so the client cannot show
    /// the player something they did not get.
    /// </summary>
    [HttpPost("RefreshShopEgoGiftsMirrorDungeon")]
    public Task<ActionResult<HttpResponseFormat<ResPacket_RefreshShopEgoGiftsMirrorDungeon>>>
        RefreshShopEgoGiftsMirrorDungeon(
            [FromBody] HttpRequestFormat<ReqPacket_RefreshShopEgoGiftsMirrorDungeon> req) =>
        ShopUnchanged<ResPacket_RefreshShopEgoGiftsMirrorDungeon>(
            req.packetId, "RefreshShopEgoGifts",
            (res, save) =>
            {
                res.cost = save.currentInfo.cost;
                res.shopInfo = save.currentInfo.shop;
            });

    [HttpPost("SendMirrorDungeonShopExit")]
    public ActionResult<HttpResponseFormat<ResPacket_SendMirrorDungeonShopExit>> SendMirrorDungeonShopExit(
        [FromBody] HttpRequestFormat<ReqPacket_SendMirrorDungeonShopExit> req) =>
        new HttpResponseFormat<ResPacket_SendMirrorDungeonShopExit>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SendMirrorDungeonShopExit(),
            packetId = req.packetId,
        };

    [HttpPost("RefreshStartEgoGiftsMirrorDungeon")]
    public Task<ActionResult<HttpResponseFormat<ResPacket_RefreshStartEgoGiftsMirrorDungeon>>>
        RefreshStartEgoGiftsMirrorDungeon(
            [FromBody] HttpRequestFormat<ReqPacket_RefreshStartEgoGiftsMirrorDungeon> req) =>
        ShopUnchanged<ResPacket_RefreshStartEgoGiftsMirrorDungeon>(
            req.packetId, "RefreshStartEgoGifts",
            (res, save) =>
            {
                res.startEgoGiftPoolSets = save.currentInfo.seps;
                res.startEgoGiftCreatedCount = save.currentInfo.sepsCreated;
            });

    [HttpPost("GetMirrorDungeonRentalFormationHistory")]
    public ActionResult<HttpResponseFormat<ResPacket_GetMirrorDungeonRentalFormationHistory>>
        GetMirrorDungeonRentalFormationHistory(
            [FromBody] HttpRequestFormat<ReqPacket_GetMirrorDungeonRentalFormationHistory> req) =>
        new HttpResponseFormat<ResPacket_GetMirrorDungeonRentalFormationHistory>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetMirrorDungeonRentalFormationHistory(),
            packetId = req.packetId,
        };

    /// <summary>Answer with the run untouched, and say in the log that nothing happened.</summary>
    private async Task<ActionResult<HttpResponseFormat<T>>> ShopUnchanged<T>(
        long packetId, string what, Action<T, MirrorDungeonSaveInfoFormat> attach)
        where T : new()
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid) ?? new MirrorDungeonSaveInfoFormat
        {
            dungeonId = -1,
            idx = -1,
            currentInfo = new MirrorDungeonCurrentInfoFormat { eid = -1 },
        };

        _logger.LogInformation("MD {What} for uid {Uid}: not implemented, run returned unchanged", what, uid);

        var result = new T();
        attach(result, save);

        return new HttpResponseFormat<T>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = result,
            packetId = packetId,
        };
    }
}
