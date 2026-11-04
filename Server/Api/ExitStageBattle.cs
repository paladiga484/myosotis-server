using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("ExitStageBattle")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_ExitStageBattle>>> ExitStageBattle(
        [FromBody] HttpRequestFormat<ReqPacket_ExitStageBattle> req)
    {
        var uid = HttpContext.GetUid();
        var iswin = req.parameters.iswin;
        var stored = await _formations.GetAsync(uid, req.parameters.formationid);
        // a hidden identity of our own (limbus-skins) is shown as the sinner's default here too
        var formation = stored is null ? null : OwnIdentities.Formations(
            [stored], OwnIdentities.Active(_config, uid, Tweak.LocalOnlyAttribute.IsLoopback(HttpContext)), _staticData.KnownPersonalities)[0];
        var levels = await _personalities.GetLevelsAsync(
            uid, formation?.formationDetails.Select(d => d.personalityId).ToList() ?? []);

        return new HttpResponseFormat<ResPacket_ExitStageBattle>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitStageBattle
            {
                stageid = req.parameters.stageid,
                iswin = iswin,
                cleartype = (int)CLEARNODE_STATE.EXCLEAR,
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
