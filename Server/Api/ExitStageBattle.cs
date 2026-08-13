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
        var formation = await _formations.GetAsync(uid, req.parameters.formationid);
        var levels = await _personalities.GetLevelsAsync(
            uid, formation?.formationDetails.Select(d => d.personalityId).ToList() ?? []);

        return new HttpResponseFormat<ResPacket_ExitStageBattle>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_ExitStageBattle
            {
                stageid = req.parameters.stageid,
                iswin = req.parameters.iswin,
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
