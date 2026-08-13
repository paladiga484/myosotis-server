using Common;
using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Login;

public partial class LoginController
{
    [HttpPost("SignInAsSteam")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SignInAsSteam>>> SignInAsSteam(
        [FromBody] HttpRequestFormat<ReqPacket_SignInAsSteam> req)
    {
        var result = await _accounts.SignInAsync(req.parameters.steamToken, "steam");
        if (result.Uid == 0)
            return StatusCode(StatusCodes.Status401Unauthorized);

        var now = TimeUtil.IsoNow();
        return new HttpResponseFormat<ResPacket_SignInAsSteam>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SignInAsSteam
            {
                walletCurrency = "IDR",
                accountInfo = new AccountInfoFormat { uid = (ulong)result.Uid },
                userAuth = new UserAuthFormat
                {
                    uid = result.Uid,
                    public_id = result.Uid,
                    db_id = 0,
                    auth_code = result.AuthCode,
                    last_login_date = now,
                    last_update_date = now,
                    data_version = 16,
                },
            },
            packetId = req.packetId,
        };
    }
}
