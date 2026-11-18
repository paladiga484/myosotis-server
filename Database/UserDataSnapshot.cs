using Types.Server;

namespace Database;

public sealed record UserDataSnapshot(
    UserInfo? UserInfo,
    List<PersonalityFormat> Personalities,
    List<EgoFormat> Egos,
    List<ItemFormat> Items,
    List<FormationFormat> Formations,
    AnnouncerFormat? Announcer,
    UserPublicProfileWithSupportersFormat? Profile,
    List<UserBannerDataFormat> Banners,
    List<UserProfileBorderFormat> LeftBorders,
    List<UserProfileBorderFormat> RightBorders,
    List<UserProfileEgobackgroundFormat> EgoBackgrounds);
