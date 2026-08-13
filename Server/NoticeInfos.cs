using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server;

// TODO: fix these? r8 now only displays first 2 lines

[ApiController]
[Route("")]
public class NoticeController : ControllerBase
{
    private static readonly NoticeMeta Meta = new()
    {
        latestUpdateDate = "Thu Aug 13 2026 23:14:36 GMT+0000 (Coordinated Universal Time)",
        noticeDetailList =
        [
            new NoticeDetailInfo
            {
                id = 200001,
                startDate = "2023-01-01T00:00:00.000Z",
                endDate = "2099-12-31T21:00:00.000Z",
                fileName_KR = "noticeDetail_200001_KR_666.json",
                fileName_EN = "noticeDetail_200001_EN_666.json",
                fileName_JP = "noticeDetail_200001_JP_666.json",
            }
        ],
    };

    private static readonly IReadOnlyDictionary<string, NoticeDetail> Details = BuildDetails(Meta);

    [HttpGet("noticemeta.json")]
    public NoticeMeta GetNoticemeta() => Meta;

    [HttpGet("noticeDetails/{fileName}")]
    public ActionResult<NoticeDetail> GetNoticeDetail(string fileName) =>
        Details.TryGetValue(fileName, out var detail) ? detail : NotFound();

    private static NoticeDetail BuildDetail(NoticeDetailInfo info) => new()
    {
        id = info.id,
        noticeType = 1,
        startDate = info.startDate,
        endDate = info.endDate,
        sprList = ["200001"],
        title = "Forget Me Not",
        content = new ContentList
        {
            list =
            [
                new ContentFormat { formatKey = "SubTitle", formatValue = "<Myosotis>" },
                new ContentFormat { formatKey = "Text", formatValue = "One only truly dies when they are forgotten." },
                new ContentFormat { formatKey = "Hyperlink", formatValue = "https://github.com/yuvlian/myosotis-server" },
                new ContentFormat { formatKey = "SubTitle", formatValue = "<Credits>" },
                new ContentFormat { formatKey = "Text", formatValue = "Main Developer: Yulian" },
                new ContentFormat { formatKey = "Text", formatValue = "W&B Inference: W.W." },
                new ContentFormat { formatKey = "Text", formatValue = "Bbygirl: Limi" },
            ],
        },
    };

    private static Dictionary<string, NoticeDetail> BuildDetails(NoticeMeta meta)
    {
        var details = new Dictionary<string, NoticeDetail>(StringComparer.Ordinal);
        foreach (var info in meta.noticeDetailList)
        {
            var detail = BuildDetail(info);
            details[info.fileName_KR] = detail;
            details[info.fileName_EN] = detail;
            details[info.fileName_JP] = detail;
        }

        return details;
    }
}
