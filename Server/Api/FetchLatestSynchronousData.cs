using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    const string NoticeTitle = "Forget Me Not";
    const string NoticeContent =
        """
        {
            "list": [
                {
                    "formatKey": "SubTitle",
                    "formatValue": "<Myosotis>"
                },
                {
                    "formatKey": "Text",
                    "formatValue": "One only truly dies when they are forgotten."
                },
                {
                    "formatKey": "Hyperlink",
                    "formatValue": "https://github.com/yuvlian/myosotis-server"
                },
                {
                    "formatKey": "SubTitle",
                    "formatValue": "<Credits>"
                },
                {
                    "formatKey": "Text",
                    "formatValue": "Main Developer: Yulian"
                },
                {
                    "formatKey": "Text",
                    "formatValue": "W&B Inference: W.W."
                },
                {
                    "formatKey": "Text",
                    "formatValue": "Bbygirl: Limi"
                }
            ]
        }
        """;

    const string MailSender = "Charon";
    const string MailSenderSpr = "charon";
    const string MailContent = "Bleh.";

    [HttpPost("FetchLatestSynchronousData")]
    public ActionResult<HttpResponseFormat<ResPacket_FetchLatestSynchronousData>> FetchLatestSynchronousData(
        [FromBody] HttpRequestFormat<ReqPacket_FetchLatestSynchronousData> req)
    {
        List<NoticeFormat> notices =
        [
            new NoticeFormat
            {
                id = 9004,
                version = 513,
                type = 0,
                startDate = "2024-11-20T00:00:00.000Z",
                endDate = "2098-12-31T21:00:00.000Z",
                sprNameList = ["200001"],
                title_KR = NoticeTitle,
                title_EN = NoticeTitle,
                title_JP = NoticeTitle,
                content_KR = NoticeContent,
                content_EN = NoticeContent,
                content_JP = NoticeContent,
            }
        ];

        List<MailContentFormat> mails =
        [
            new MailContentFormat
            {
                id = 5858,
                version = 9004,
                senderSprName = MailSenderSpr,
                sender_KR = MailSender,
                sender_EN = MailSender,
                sender_JP = MailSender,
                content_KR = MailContent,
                content_EN = MailContent,
                content_JP = MailContent,
            }
        ];

        return new HttpResponseFormat<ResPacket_FetchLatestSynchronousData>()
        {
            serverInfo = new ServerInfo
            {
                version = "product"
            },
            state = "ok",
            synchronized = new SynchronizedFormat
            {
                version = 513,
                noticeList = notices,
                mailContentList = mails,
            },
            packetId = req.packetId
        };
    }
}
