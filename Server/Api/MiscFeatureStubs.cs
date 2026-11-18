using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

/// <summary>
/// Features the client reaches for that this server does not implement.
///
/// These all showed up as empty 404s during real play, which the client cannot deserialise -
/// so instead of the feature merely being absent, whatever screen asked for it broke. A
/// well-formed empty answer lets the client render "nothing here" and carry on.
///
/// Nothing here fakes success. Where a reward would be granted, none is, and the log says so.
/// </summary>
public partial class ApiController
{
    /// <summary>Event reward claim. No event system exists, so nothing is granted.</summary>
    [HttpPost("ClaimEventReward")]
    public ActionResult<HttpResponseFormat<ResPacket_ClaimEventReward>> ClaimEventReward(
        [FromBody] HttpRequestFormat<ReqPacket_ClaimEventReward> req)
    {
        _logger.LogInformation(
            "ClaimEventReward event {EventId} reward {RewardId}: events are not implemented, nothing granted",
            req.parameters.eventId, req.parameters.eventRewardId);

        return new HttpResponseFormat<ResPacket_ClaimEventReward>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ClaimEventReward { acquiredElements = [] },
            packetId = req.packetId,
        };
    }

    [HttpPost("ClaimEventReward_ALL")]
    public ActionResult<HttpResponseFormat<ResPacket_ClaimEventReward_ALL>> ClaimEventRewardAll(
        [FromBody] HttpRequestFormat<ReqPacket_ClaimEventReward_ALL> req)
    {
        _logger.LogInformation(
            "ClaimEventReward_ALL event {EventId}: events are not implemented, nothing granted",
            req.parameters.eventId);

        return new HttpResponseFormat<ResPacket_ClaimEventReward_ALL>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ClaimEventReward_ALL { acquiredElements = [] },
            packetId = req.packetId,
        };
    }

    /// <summary>
    /// Leaving a Refraction Railway run. The save is returned as it stands so the client can close
    /// the run cleanly instead of hanging on the exit screen.
    /// </summary>
    [HttpPost("ExitRailwayDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitRailwayDungeon>>> ExitRailwayDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_ExitRailwayDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _railway.GetSaveAsync(uid, req.parameters.dungeonId);

        _logger.LogInformation(
            "ExitRailwayDungeon uid {Uid} dungeon {DungeonId}: isClear={IsClear}, no rewards granted",
            uid, req.parameters.dungeonId, req.parameters.isClear);

        return new HttpResponseFormat<ResPacket_ExitRailwayDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitRailwayDungeon
            {
                isclear = req.parameters.isClear,
                saveInfo = save ?? new RailwayDungeonSaveInfoFormat { id = req.parameters.dungeonId },
                currentLog = new RailwayLogDataFormat(),
                rewards = [],
            },
            packetId = req.packetId,
        };
    }

    /// <summary>Boss Raid is not implemented; an empty save reads as "no raid in progress".</summary>
    [HttpPost("GetBossRaidSaveInfo")]
    public ActionResult<HttpResponseFormat<ResPacket_GetBossRaidSaveInfo>> GetBossRaidSaveInfo(
        [FromBody] HttpRequestFormat<ReqPacket_GetBossRaidSaveInfo> req) =>
        new HttpResponseFormat<ResPacket_GetBossRaidSaveInfo>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetBossRaidSaveInfo
            {
                saveInfo = new BossRaidSaveDataFormat(),
                partyDatas = [],
                logData = new BossRaidLogDataFormat(),
            },
            packetId = req.packetId,
        };

    /// <summary>
    /// Friend support units. There are no friends on a single-machine server, so the list is
    /// empty - which is a real state the client handles, unlike a 404.
    /// </summary>
    [HttpPost("GetFriendSupportPersonalitiesByCharacterId")]
    public ActionResult<HttpResponseFormat<ResPacket_GetFriendSupportPersonalitiesByCharacterId>>
        GetFriendSupportPersonalitiesByCharacterId(
            [FromBody] HttpRequestFormat<ReqPacket_GetFriendSupportPersonalitiesByCharacterId> req) =>
        new HttpResponseFormat<ResPacket_GetFriendSupportPersonalitiesByCharacterId>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetFriendSupportPersonalitiesByCharacterId { supportpersonalities = [] },
            packetId = req.packetId,
        };
}
