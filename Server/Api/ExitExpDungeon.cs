using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("ExitExpDungeonv2")]
    [HttpPost("ExitExpDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitExpDungeon>>> ExitExpDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_ExitExpDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var stored = await _formations.GetAsync(uid, req.parameters.formationId);
        // a hidden identity of our own (limbus-skins) is shown as the sinner's default here too
        var formation = stored is null ? null : OwnIdentities.Formations(
            [stored], OwnIdentities.Active(_config, uid, BlackForest.IsLocal(HttpContext)), _staticData.KnownPersonalities)[0];
        var levels = await _personalities.GetLevelsAsync(
            uid, formation?.formationDetails.Select(d => d.personalityId).ToList() ?? []);

        return new HttpResponseFormat<ResPacket_ExitExpDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitExpDungeon
            {
                clearInfo = new ExpDungeonClearInfoFormat
                {
                    clearnumber = (uint)req.parameters.isWin,
                },
                personalityinfos = formation is null
                    ? []
                    : formation.formationDetails.Select(d => new StagePersonalityInfoFormat
                    {
                        personalityid = d.personalityId,
                        prevlevel = levels.GetValueOrDefault(d.personalityId, 1),
                    }).ToList(),
            },
            packetId = req.packetId,
        };
    }
}
