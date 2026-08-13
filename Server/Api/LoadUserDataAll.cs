using Common;
using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("LoadUserDataAll")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_LoadUserDataAll>>> LoadUserDataAll(
        [FromBody] HttpRequestFormat<ReqPacket_LoadUserDataAll> req)
    {
        var uid = HttpContext.GetUid();
        var snapshot = await _db.GetUserDataSnapshotAsync(uid);

        var updated = new UpdatedFormat
        {
            isInitialized = true,
            userInfo = snapshot.UserInfo,
            personalityList = snapshot.Personalities,
            egoList = snapshot.Egos,
            formationList = snapshot.Formations,
            lobbyCG = new LobbyCgFormat
            {
                characterId = 1,
                isShowProfile = true,
            },
            itemList = snapshot.Items,
            chanceList = _staticData.Chances.ToList(),
            battlePass = _staticData.BattlePass ?? new BattlePassFormat(),
            mainChapterStateList = _staticData.MainChapterStates.ToList(),
            mailList = [],
            announcer = snapshot.Announcer,
            membershipList = _staticData.Memberships.ToList(),
            gachaList = [],
            userUnlockCodeList = _staticData.UnlockCodes.ToList(),
            eventRewardStateList = [],
            isUpdateUserBanner = false,
            isResetMirrorDungeon = false,
            missionList = [],
            danteAbilityList =
            [
                new DanteAbilityFormat
                {
                    category = DANTE_ABILITY_CATEGORY.DEFAULT,
                    abilityids = _staticData.DanteAbilityIds.ToList(),
                    remaincount = 1,
                },
                new DanteAbilityFormat
                {
                    category = DANTE_ABILITY_CATEGORY.RAILWAY_DUNGEON,
                    abilityids = _staticData.DanteAbilityIds.ToList(),
                    remaincount = 1,
                },
            ],
        };

        var result = new ResPacket_LoadUserDataAll
        {
            profile = snapshot.Profile ?? new UserPublicProfileWithSupportersFormat(),
            danteNoteTodayPage = 49,
            dailyLoginRewardStates = [],
            dailyLoginWeekId = -1,
            showedWeekByMinistory = -1,
            userbanners = snapshot.Banners,
            leftBorders = snapshot.LeftBorders,
            rightBorders = snapshot.RightBorders,
            egoBackgrounds = snapshot.EgoBackgrounds,
            date = TimeUtil.IsoNow(),
        };

        return new HttpResponseFormat<ResPacket_LoadUserDataAll>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = updated,
            result = result,
            synchronized = new SynchronizedFormat
            {
                version = req.userAuth.synchronousDataVersion + 17,
                noticeList = [],
                mailContentList = [],
            },
            packetId = req.packetId,
        };
    }
}
