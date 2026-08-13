using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Login;

public partial class LoginController
{
    [HttpPost("GetTermsOfUseStateAll")]
    public ActionResult<HttpResponseFormat<ResPacket_GetTermsOfUseStateAll>> GetTermsOfUseStateAll(
        [FromBody] HttpRequestFormat<ReqPacket_GetTermsOfUseStateAll> req)
    {
        return new HttpResponseFormat<ResPacket_GetTermsOfUseStateAll>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_GetTermsOfUseStateAll
            {
                termsOfUseStateList =
                [
                    new TermsOfUseState
                    {
                        version = 1,
                        state = (int)TERMSOFUSE_STATE.AGREE,
                    },
                ],
            },
            packetId = req.packetId,
        };
    }
}
