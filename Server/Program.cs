using Types.Server;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/refresh-start-ego-gifts", () =>
{
    return new ResPacket_RefreshStartEgoGiftsStoryMirrorDungeon
    {
        startEgoGiftPoolSets = [],
        startEgoGiftCreatedCount = 0
    };
});

app.Run();
