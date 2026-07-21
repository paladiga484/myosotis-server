using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Login;

public partial class LoginController
{
    [HttpPost("SignInAsSteam")]
    public ActionResult<HttpResponseFormat<ResPacket_SignInAsSteam>> SignInAsSteam(
        [FromBody] HttpRequestFormat<ReqPacket_SignInAsSteam> req)
    {
        return new HttpResponseFormat<ResPacket_SignInAsSteam>()
        {
            serverInfo = new ServerInfo
            {
                version = "product"
            },
            state = "ok",
            result = new ResPacket_SignInAsSteam
            {
                walletCurrency = "USD",
                accountInfo = new AccountInfoFormat
                {
                    uid = 1,
                },
                userAuth = new UserAuthFormat
                {
                    uid = 1,
                    public_id = 1,
                    db_id = 0,
                    auth_code = req.parameters.steamToken,
                    last_login_date = "2025-03-31T15:10:00.000Z",
                    last_update_date = "2025-03-31T15:10:00.000Z",
                    data_version = 16,
                }
            },
            packetId = req.packetId
        };
    }
}
