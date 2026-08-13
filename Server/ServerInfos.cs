using Common;
using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server;

[ApiController]
[Route("")]
public class ServerInfosController(Config config) : ControllerBase
{
    [HttpGet("serverinfos.json")]
    public ActionResult<ServerDispatchInfo[]> Get()
    {
        return new ServerDispatchInfo[]
        {
            new ServerDispatchInfo
            {
                platform = "windows",
                serviceType = "product",
                serverId = "win_product",
                versions = config.Server.Versions,
                serverUrl = "https://www.limbuscompanyapi.com",
                logServerUrl = "https://battlelog.limbuscompanyapi.com",
                presenceServerUrl = "https://presence.limbuscompanyapi.com",
                noticeUrl = "https://notice.limbuscompanyapi.com",
                cdnUrl = "",
                guestAuthErrors = [101, 15003, 40004, 40005],
                enablePacketCrypt = true,
                enableLogPacketCrypt = true,
                enableAgreementVersionCheck = true,
                enableNetworkingUIProcessPurchase = true,
                enableCheckUpdateCatalogSteamFixed = true,
                enableCheckUpdateCatalogToTitle = false,
                enableCheckUpdateTextAsset = true,
                enableCheckError = true,
                enableBLogSync = true,
            },
        };
    }
}
