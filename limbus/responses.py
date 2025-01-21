from pydantic import BaseModel
from typing import List, Optional, Generic, TypeVar
from limbus.formats import *

A = TypeVar("A")


# class RspUpdateTermsOfUseVersion(BaseModel):
#     uid: int
#     termsVersion: int
#     state: int
class Sc(BaseModel, Generic[A]):
    serverInfo: ServerInfo = ServerInfo()
    state: str = "ok"
    updated: Optional[UpdatedFormat] = None
    synchronized: Optional[SynchronizedFormat] = None
    result: Optional[A] = None


class RspNull(BaseModel):
    pass


class RspGetHellsChickenState(BaseModel):
    dollsNum: int = 0
    rewardState: List[int] = []


class RspAcquireHellsChickenReward(BaseModel):
    rewardState: int = 0
    rewards: List[Element] = []


class RspGetAttendanceState(BaseModel):
    rewardState: List[int] = []
    consumption: int = 0


class RspAcquireAttendanceReward(BaseModel):
    rewardState: List[int] = []
    rewards: List[Element] = []


class RspBattlePassReward(BaseModel):
    resultElements: List[Element] = []


class RspBattlePassExLevelReward(BaseModel):
    resultElements: List[Element] = []


class RspUseCoupon(BaseModel):
    state: int = 0
    rewards: Optional[List[Element]] = None
    backoffdate: Optional[str] = None
    backoffduration: Optional[int] = None


class RspGetUserCouponState(BaseModel):
    ispossiblestate: bool = False
    backoffdate: str = ""
    backoffduration: int = 0


class RspEnterExpDungeon(BaseModel):
    isclear: int = 0


class RspExitExpDungeon(BaseModel):
    userExp: int = 0
    personalityinfos: List[StagePersonalityInfoFormat] = []
    acquiredtickets: List[Element] = []
    rewards: List[Element] = []
    clearInfo: ExpDungeonClearInfoFormat = ExpDungeonClearInfoFormat()


class RspSkipExpDungeon(BaseModel):
    userExp: int = 0
    rewards: List[Element] = []


class RspEnterThreadDungeon(BaseModel):
    isClear: int = 0
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspExitThreadDungeon(BaseModel):
    userExp: int = 0
    rewards: List[Element] = []
    clearInfo: ThreadDungeonClearInfoFormat = ThreadDungeonClearInfoFormat()


class RspGetDailyDungeonInfo(BaseModel):
    expDungeonClearInfo: List[ExpDungeonClearInfoFormat] = []
    threadDungeonClearInfo: List[ThreadDungeonClearInfoFormat] = []
    date: str = ""


class RspSkipThreadDungeon(BaseModel):
    userExp: int = 0
    rewards: List[Element] = []


class RspGetDailyLoginState(BaseModel):
    weekid: int = 0
    id: int = 0
    rewardstates: List[DailyLoginRewardStateFormat] = []


class RspAcquireDailyLoginReward(BaseModel):
    rewards: List[Element] = []


class RspGetDanteNoteState(BaseModel):
    page: int = 0
    todayPage: int = 0


class RspGetStageProgressRateRewardCommand(BaseModel):
    rewardList: List[Element] = []


class RspEnterStageBattleCommand(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspExitStageBattleCommand(BaseModel):
    stageid: int
    iswin: bool
    cleartype: int
    addexptouser: int = 0
    personalityinfos: List[StagePersonalityInfoFormat]
    expticket: Optional[List[Element]] = None
    rewarditem: Optional[List[Element]] = None
    exrewarditem: Optional[List[Element]] = None
    firstrewarditem: Optional[List[Element]] = None
    givebackstaminabyDefeat: Optional[Element] = None
    abnormalityLogs: Optional[List[AbnormalityUnlockInformationFormat]] = None


class RspGetDungeonSaveInfoAll(BaseModel):
    storySaveInfo: StoryDungeonSaveInfoFormat = StoryDungeonSaveInfoFormat()
    mirrorOriginSaveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()
    railwaySaveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    storyMirrorSaveInfo: StoryMirrorDungeonSaveInfoFormat = (
        StoryMirrorDungeonSaveInfoFormat()
    )
    mirrorDungeonClearInfos: List[MirrorDungeonClearInfoFormat] = []
    mirrorDungeonHistories: List[MirrorDungeonHistoryFormat] = []


class RspGetStoryDungeonSaveInfo(BaseModel):
    saveInfo: StoryDungeonSaveInfoFormat = StoryDungeonSaveInfoFormat()


class RspEnterStoryDungeon(BaseModel):
    saveInfo: StoryDungeonSaveInfoFormat = StoryDungeonSaveInfoFormat()
    nodesRecord: List[int] = []


class RspReEnterStoryDungeon(BaseModel):
    saveInfo: StoryDungeonSaveInfoFormat = StoryDungeonSaveInfoFormat()
    nodesRecord: List[int] = []
    isAllDie: int = 0


class RspExitStoryDungeonCommand(BaseModel):
    saveInfo: StoryDungeonSaveInfoFormat = StoryDungeonSaveInfoFormat()
    iswin: bool = False
    cleartype: int = 0
    addexptouser: int = 0
    personalityinfos: List[StagePersonalityInfoFormat] = []
    expticket: List[Element] = []
    rewarditem: List[Element] = []
    exrewarditem: List[Element] = []
    firstrewarditem: List[Element] = []
    givebackstaminabyDefeat: Element = Element()
    statistics: List[DungeonStatisticsDataFormat] = []
    isGacksung: bool = False


class RspEnterStoryDungeonMapNodeCommand(BaseModel):
    node: DungeonMapNodeFormat = DungeonMapNodeFormat()
    nr: int = 0
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspUpdateStoryDungeonMapNodeCommand(BaseModel):
    prevChoiceEvent: List[DungeonChoiceEventSaveDataFormat] = []
    currentEgoGifts: List[DungeonMapEgoGiftFormat] = []


class RspEnterStoryDungeonMapNodeBattleAfterChoice(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    isAllDie: int = 0


class RspExitStoryDungeonMapNodeCommand(BaseModel):
    saveInfo: StoryDungeonSaveInfoFormat = StoryDungeonSaveInfoFormat()
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    acquiredEgogifts: List[DungeonMapEgoGiftFormat] = []
    isAllDie: int = 0


class RspReturnSavePointStoryDungeonMap(BaseModel):
    currentInfo: StoryDungeonCurrentInfoFormat = StoryDungeonCurrentInfoFormat()


class RspExitStoryDungeonMapNodeByForcely(BaseModel):
    currentInfo: StoryDungeonCurrentInfoFormat = StoryDungeonCurrentInfoFormat()
    isAllDie: int = 0


class RspExitStoryCommand(BaseModel):
    rewarditem: List[Element] = []
    exrewarditem: List[Element] = []


class RspGetAbnormalityLogData(BaseModel):
    logdatas: List[AbnormalityUnlockInformationFormat] = []


class RspUpdateAbnormalityLogData(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspGetStoryDungeonNodeRecord(BaseModel):
    nodes: List[int] = []


class RspClaimEventRewardCommand(BaseModel):
    acquiredElements: List[Element] = []


class RspClaimEventReward_ALLCommand(BaseModel):
    acquiredElements: List[Element] = []


class RspPlayGacha(BaseModel):
    gachaLogDetails: List[GachaLogDetail] = []


class RspGetGachaLogAll(BaseModel):
    gachaLogs: List[GachaLog] = []


class RspUseGachaPityPoint(BaseModel):
    gachaLogDetails: List[GachaLogDetail] = []


class RspClaimClosedGachaRewards(BaseModel):
    pityPointDataList: Optional[List[int]] = None


class RspInitPurchase(BaseModel):
    resultState: str = ""


class RspUpdateSteamPendingPurchase(BaseModel):
    finalizedTransactionIds: List[str] = []


class RspGetSteamWalletCurrency(BaseModel):
    walletCurrency: str = ""


class RspUseItem(BaseModel):
    pickedUpElementList: List[Element] = []
    resultElementList: List[Element] = []


class RspUsePersonalityExpItem(BaseModel):
    resultPersonality: PersonalityFormat = PersonalityFormat()


class RspLoadUserDataAll(BaseModel):
    secession_Date: str = ""
    profile: UserPublicProfileWithSupportersFormat = (
        UserPublicProfileWithSupportersFormat()
    )
    danteNoteTodayPage: int = 0
    dailyLoginRewardStates: List[DailyLoginRewardStateFormat] = []
    dailyLoginWeekId: int = 0
    dailyLoginId: int = 0
    showedWeekByMinistory: int = 0
    date: str = ""


class RspCheckSeasonLog(BaseModel):
    seasonLog: SeasonLogFormat = SeasonLogFormat()


class RspSignInAsGuest(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()


class RspSignInAsNewGuest(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()
    authToken: str = ""


class RspSignInAsGoogle(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()
    accountInfo: AccountInfoFormat = AccountInfoFormat()


class RspLinkWithGoogle(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()
    accountInfo: AccountInfoFormat = AccountInfoFormat()


class RspSignInAsApple(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()
    accountInfo: AccountInfoFormat = AccountInfoFormat()


class RspLinkWithApple(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()
    accountInfo: AccountInfoFormat = AccountInfoFormat()


class RspSignInAsSteam(BaseModel):
    userAuth: UserAuthFormat = UserAuthFormat()
    accountInfo: AccountInfoFormat = AccountInfoFormat()
    walletCurrency: str = ""


class RspRefreshLinkAuth(BaseModel):
    linkAuth: LinkAuthFormat = LinkAuthFormat()
    state: str = ""


class RspGetInfoOfLinkWith(BaseModel):
    details: str = ""
    state: str = ""


class RspLinkWithAnother(BaseModel):
    state: str = ""


class RspTryToSecede(BaseModel):
    secessionDate: str = ""


class RspCheckClientVersion(BaseModel):
    timeoffset: int = 0


class RspGetBanDetails(BaseModel):
    startDate: str = ""
    endDate: str = ""
    reason: str = ""


class RspUnLinkWithAnother(BaseModel):
    accountInfo: AccountInfoFormat = AccountInfoFormat()


class RspRefreshMailBox(BaseModel):
    initializedMailList: List[MailFormat] = []


class RspUnsealMails(BaseModel):
    attachedElements: List[Element] = []


class RspGetMailLogAll(BaseModel):
    mailLogs: List[MailLog] = []


class RspGetMirrorDungeonSaveInfoAll(BaseModel):
    originSaveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()
    simulationsaveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspGetMirrorDungeonSaveInfo(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspEnterMirrorDungeonCommand(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()
    recentCharacterList: List[MirrorDungeonGetCharacterInfoFormat] = []


class RspReEnterMirrorDungeonCommand(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspUpdateMirrorDungeonCommand(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspSelectFormationAndCreateThemePoolMirrorDungeonCommand(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspRecreateThemeFloorPoolMirrorDungeonCommand(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspSelectThemeFloorMirrorDungeonCommand(BaseModel):
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspExitMirrorDungeonCommand(BaseModel):
    isEndDungeon: int = 0
    isclear: int = 0
    statistics: List[DungeonStatisticsDataFormat] = []


class RspAcquireMirrorDungeonLastReward(BaseModel):
    rewardList: List[Element] = []
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()
    acquiredChip: MirrorDungeonAcquiredChipFormat = MirrorDungeonAcquiredChipFormat()
    history: MirrorDungeonHistoryFormat = MirrorDungeonHistoryFormat()
    startBuffInfo: MirrorDungeonStartBuffInfoFormat = MirrorDungeonStartBuffInfoFormat()


class RspEnterMirrorDungeonMapNodeCommand(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    passingNodeIds: List[int] = []
    currentNode: DungeonMapNodeFormat = DungeonMapNodeFormat()
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()
    egogifts: List[DungeonMapEgoGiftFormat] = []
    prevdul: List[MirrorDungeonPrevUnitInfoFormat] = []
    preves: List[int] = []


class RspUpdateMirrorDungeonMapNodeCommand(BaseModel):
    prevChoiceEvent: List[DungeonChoiceEventSaveDataFormat] = []
    currentEgoGifts: List[DungeonMapEgoGiftFormat] = []
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []


class RspEnterMirrordungeonMapNodeBattleAfterChoice(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspExitMirrorDungeonMapNodeCommand(BaseModel):
    currentInfo: MirrorDungeonCurrentInfoFormat = MirrorDungeonCurrentInfoFormat()
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspExitMirrorDungeonMapNodeByForcely(BaseModel):
    currentInfo: MirrorDungeonCurrentInfoFormat = MirrorDungeonCurrentInfoFormat()


class RspAcquireRewardEgoGiftsMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    saveinfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspAcquireRewardEgoGiftsWithEnemyBufMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    levelAdders: List[int] = []
    saveinfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspRejectRewardEgoGiftsMirrorDungeonCommand(BaseModel):
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []
    saveinfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspRejectRewardEgoGiftWithEnemyBufsMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    levelAdders: List[int] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []
    saveinfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspAcquireCharacterEventDataMirrorDungeonCommand(BaseModel):
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspPersonalityLevelUpEventMirrorDungeonCommand(BaseModel):
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspAcquireMirrorDungeonBattleRewardCommand(BaseModel):
    saveinfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()


class RspGetMirrorDungeonEgoGiftRecordCommand(BaseModel):
    acquiredegogifts: List[int] = []


class RspPurchaseHealMirrorDungeon(BaseModel):
    cost: int = 0
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()


class RspPurchaseFormationMirrorDungeon(BaseModel):
    cost: int = 0
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()
    prevUnitInfo: MirrorDungeonPrevUnitInfoFormat = MirrorDungeonPrevUnitInfoFormat()


class RspPurchaseUpgradePersonalityMirrorDungeon(BaseModel):
    cost: int = 0
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()


class RspPurchaseEgoGiftMirrorDungeon(BaseModel):
    cost: int = 0
    egogifts: List[DungeonMapEgoGiftFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []


class RspSellEgoGift(BaseModel):
    cost: int = 0
    egogifts: List[DungeonMapEgoGiftFormat] = []


class RspRefreshShopEgoGiftsMirrorDungeonCommand(BaseModel):
    cost: int = 0
    consumedChipIn: int = 0
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()


class RspGetStartBuffFInfoMirrorDungeon(BaseModel):
    startBuffInfo: MirrorDungeonStartBuffInfoFormat = MirrorDungeonStartBuffInfoFormat()


class RspPurchaseStartBuffMirrorDungeon(BaseModel):
    startBuffInfo: MirrorDungeonStartBuffInfoFormat = MirrorDungeonStartBuffInfoFormat()


class RspEnableSpecialStartBuffMirrorDungeon(BaseModel):
    startBuffInfo: MirrorDungeonStartBuffInfoFormat = MirrorDungeonStartBuffInfoFormat()


class RspDisableStartBuffMirrorDungeon(BaseModel):
    startBuffInfo: MirrorDungeonStartBuffInfoFormat = MirrorDungeonStartBuffInfoFormat()


class RspRemoveMirrorDungeonEgoGift(BaseModel):
    egs: List[DungeonMapEgoGiftFormat] = []


class RspMirrorDungeonGiveUpSelectingEgoGift(BaseModel):
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspGetMirrorDungeonPreset(BaseModel):
    recentCharacterList: List[MirrorDungeonFormationFormat] = []


class RspGetMirrorDungeonRewardChip(BaseModel):
    acquiredChip: MirrorDungeonAcquiredChipFormat = MirrorDungeonAcquiredChipFormat()
    rewardList: List[MirrorDungeonAcquiredRewardFormat] = []


class RspSelectMirrorDungeonRandomPickFormation(BaseModel):
    formation: List[MirrorDungeonFormationFormat] = []


class RspAcquireStartEgoGiftsMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    startEgoGiftPoolSets: List[MirrorDungeonEgoGiftPoolSetFormat] = []
    startEgoGiftCreatedCount: int = 0


class RspRefreshStartEgoGiftsMirrorDungeonCommand(BaseModel):
    startEgoGiftPoolSets: List[MirrorDungeonEgoGiftPoolSetFormat] = []
    startEgoGiftCreatedCount: int = 0


class RspUpgradeEgoGiftMirrorDungeonCommand(BaseModel):
    cost: int = 0
    egoGift: DungeonMapEgoGiftFormat = DungeonMapEgoGiftFormat()
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []


class RspCombineEgoGiftMirrorDungeonCommand(BaseModel):
    resultEgoGift: DungeonMapEgoGiftFormat = DungeonMapEgoGiftFormat()
    isSuccess: bool = False
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    dungeonUnitList: List[MirrorDungeonSaveUnitInfoFormat] = []


class RspPreviewMirrorDungeonExitReward(BaseModel):
    acquiredChip: MirrorDungeonAcquiredChipFormat = MirrorDungeonAcquiredChipFormat()
    rewardList: List[MirrorDungeonExitRewardFormat] = []


class RspAcquireMirrorDungeonExitReward(BaseModel):
    rewardList: List[Element] = []
    saveInfo: MirrorDungeonSaveInfoFormat = MirrorDungeonSaveInfoFormat()
    acquiredChip: MirrorDungeonAcquiredChipFormat = MirrorDungeonAcquiredChipFormat()
    history: MirrorDungeonHistoryFormat = MirrorDungeonHistoryFormat()
    startBuffInfo: MirrorDungeonStartBuffInfoFormat = MirrorDungeonStartBuffInfoFormat()


class RspAcquireMissionRewardsCommand(BaseModel):
    acquiredElements: List[Element] = []


class RspGetTermsOfUseStateAll(BaseModel):
    version: int = 0
    termsOfUseStateList: List[TermsOfUseState] = []


class RspGetTheaterInfo(BaseModel):
    theaterInfo: UserTheaterInfoFormat = UserTheaterInfoFormat()


class RspCompleteTheaterStory(BaseModel):
    isRewarded: bool = False
    acquiredElements: List[Element] = []
    theaterInfo: UserTheaterInfoFormat = UserTheaterInfoFormat()


class RspGetUserBanners(BaseModel):
    banners: List[UserBannerDataFormat] = []


class RspGetFriendsData(BaseModel):
    friendprofileList: List[UserPublicProfileFormat] = []
    sendprofileList: List[UserPublicProfileFormat] = []
    receiveprofileList: List[UserPublicProfileFormat] = []


class RspFindFriend(BaseModel):
    success: bool = False
    friendprofile: UserPublicProfileFormat = UserPublicProfileFormat()


class RspGetRecommendFriends(BaseModel):
    recomendedFriends: List[UserPublicProfileFormat] = []


class RspSendFriendRequest(BaseModel):
    success: int = 0
    receiverprofile: UserPublicProfileFormat = UserPublicProfileFormat()


class RspAcceptReceivedFriendRequest(BaseModel):
    success: int = 0


class RspGetFriendSupportPersonalities(BaseModel):
    supportpersonalities: List[SupportPersonalitySlotFormat] = []


class RspGetFriendSupportPersonalitiesByCharacterId(BaseModel):
    supportpersonalities: List[SupportPersonalityFormat] = []


class RspGetProfileTicketDecoDatas(BaseModel):
    leftBorders: List[UserProfileBorderFormat] = []
    rightBorders: List[UserProfileBorderFormat] = []
    egoBackgrounds: List[UserProfileEgobackgroundFormat] = []


class RspUpdateProfileTicketDeco(BaseModel):
    leftBorderId: int = 0
    rightBorderId: int = 0
    egoBackgroundId: int = 0


class RspPlayVendingMachine(BaseModel):
    itemConsumptions: List[ItemFormat] = []


class RspAcquireEgoGiftEventStoryMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspAcquireCharacterEventDataStoryMirrorDungeonCommand(BaseModel):
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspPersonalityLevelUpEventStoryMirrorDungeonCommand(BaseModel):
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspEnterStoryMirrorDungeonCommand(BaseModel):
    saveInfo: StoryMirrorDungeonSaveInfoFormat = StoryMirrorDungeonSaveInfoFormat()


class RspReEnterStoryMirrorDungeonCommand(BaseModel):
    saveInfo: StoryMirrorDungeonSaveInfoFormat = StoryMirrorDungeonSaveInfoFormat()


class RspUpdateStoryMirrorDungeonCommand(BaseModel):
    saveInfo: StoryMirrorDungeonSaveInfoFormat = StoryMirrorDungeonSaveInfoFormat()


class RspExitStoryMirrorDungeonCommand(BaseModel):
    saveInfo: StoryMirrorDungeonSaveInfoFormat = StoryMirrorDungeonSaveInfoFormat()
    isclear: int = 0
    statistics: List[DungeonStatisticsDataFormat] = []
    cleartype: int = 0
    adduserexp: int = 0
    personalityinfos: List[StagePersonalityInfoFormat] = []
    normalrewards: List[Element] = []
    exrewards: List[Element] = []
    firstrewarditem: List[Element] = []
    expticket: List[Element] = []
    givebackstaminabyDefeat: Element = Element()


class RspEnterStoryMirrorDungeonMapNodeCommand(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    passingNodeIds: List[int] = []
    currentNode: DungeonMapNodeFormat = DungeonMapNodeFormat()
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    prevdul: List[MirrorDungeonPrevUnitInfoFormat] = []
    preves: List[int] = []


class RspUpdateStoryMirrorDungeonMapNodeCommand(BaseModel):
    prevChoiceEvent: List[DungeonChoiceEventSaveDataFormat] = []
    currentEgoGifts: List[DungeonMapEgoGiftFormat] = []
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []


class RspEnterStoryMirrorDungeonMapNodeBattleAfterChoiceCommand(BaseModel):
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspExitStoryMirrorDungeonMapNodeCommand(BaseModel):
    currentInfo: StoryMirrorDungeonCurrentInfoFormat = (
        StoryMirrorDungeonCurrentInfoFormat()
    )
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspAcquireRewardEgoGiftsStoryMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []


class RspRejectRewardEgoGiftsStoryMirrorDungeonCommand(BaseModel):
    remainRewardEvent: List[RandomDungeonEncounterRewardEventInfoFormat] = []


class RspPurchaseHealStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()


class RspPurchaseFormationStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()
    prevUnitInfo: MirrorDungeonPrevUnitInfoFormat = MirrorDungeonPrevUnitInfoFormat()


class RspPurchaseUpgradePersonalityStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()


class RspPurchaseEgoGiftStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    egogifts: List[DungeonMapEgoGiftFormat] = []
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []


class RspSellEgoGiftStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    egogifts: List[DungeonMapEgoGiftFormat] = []


class RspAcquireStartEgoGiftsStoryMirrorDungeonCommand(BaseModel):
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    startEgoGiftPoolSets: List[MirrorDungeonEgoGiftPoolSetFormat] = []
    startEgoGiftCreatedCount: int = 0


class RspUpgradeEgoGiftStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    egoGift: DungeonMapEgoGiftFormat = DungeonMapEgoGiftFormat()
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []


class RspCombineEgoGiftStoryMirrorDungeonCommand(BaseModel):
    resultEgoGift: DungeonMapEgoGiftFormat = DungeonMapEgoGiftFormat()
    isSuccess: bool = False
    egoGifts: List[DungeonMapEgoGiftFormat] = []
    dungeonUnitList: List[StoryMirrorDungeonSaveUnitInfoFormat] = []


class RspRefreshStartEgoGiftsStoryMirrorDungeonCommand(BaseModel):
    startEgoGiftPoolSets: List[MirrorDungeonEgoGiftPoolSetFormat] = []
    startEgoGiftCreatedCount: int = 0


class RspRefreshShopEgoGiftsStoryMirrorDungeonCommand(BaseModel):
    cost: int = 0
    shopInfo: UserMirrorDungeonShopDataFormat = UserMirrorDungeonShopDataFormat()


class RspRemoveStoryMirrorDungeonEgoGift(BaseModel):
    egs: List[DungeonMapEgoGiftFormat] = []


class RspEnterRailwayDungeon(BaseModel):
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    startNodeData: RailwayNodeDataFormat = RailwayNodeDataFormat()


class RspEnterRailwayDungeonMapNodeCommand(BaseModel):
    nodeid: int = 0
    deletedNodeIds: List[int] = []
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    prevStatusData: List[RailwayUnitStatusFormat] = []
    prevEgoStockData: List[RailwayEGOStockFormat] = []
    prevEnemyData: SaveDataForRailwayDungeon = SaveDataForRailwayDungeon()
    prevClearNodeId: int = 0
    currentNodeId: int = 0


class RspGetRailwayDungeonNodeAndLogAllCommand(BaseModel):
    nodeDatas: List[RailwayNodeDataFormat] = []
    logDatas: List[RailwayLogDataFormat] = []


class RspExitRailwayDungeonMapNodeCommand(BaseModel):
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    nodeData: RailwayNodeDataFormat = RailwayNodeDataFormat()
    updateNodeDatas: List[RailwayNodeDataFormat] = []


class RspExitRailwayDungeonRestNodeCommand(BaseModel):
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    deletedNodeIds: List[int] = []
    nodeData: RailwayNodeDataFormat = RailwayNodeDataFormat()


class RspExitRailwayDungeonCommand(BaseModel):
    isclear: bool = False
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    currentLog: RailwayLogDataFormat = RailwayLogDataFormat()
    rewards: List[Element] = []


class RspGiveUpRailwayDungeonNodeCommand(BaseModel):
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []
    nodeData: RailwayNodeDataFormat = RailwayNodeDataFormat()


class RspGiveUpRailwayDungeonNodeInBattle(BaseModel):
    nodeData: RailwayNodeDataFormat = RailwayNodeDataFormat()
    abnormalityLogs: List[AbnormalityUnlockInformationFormat] = []


class RspSelectRailwayDungeonBuffCommand(BaseModel):
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    nodeData: RailwayNodeDataFormat = RailwayNodeDataFormat()


class RspAcquireRailwayDungeonReward(BaseModel):
    saveInfo: RailwayDungeonSaveInfoFormat = RailwayDungeonSaveInfoFormat()
    rewardList: List[Element] = []
