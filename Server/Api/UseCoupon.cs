using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("UseCoupon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_UseCoupon>>> UseCoupon(
        [FromBody] HttpRequestFormat<ReqPacket_UseCoupon> req)
    {
        var uid = HttpContext.GetUid();
        var code = req.parameters.code;
        if (code.Length == 0)
            return Ok(UseCouponResponse(2, req.packetId));

        switch (code[0])
        {
            case 'R':
                {
                    if (code != "RESET" && !_config.Command.AllowResetDbCommand)
                        return Ok(UseCouponResponse(2, req.packetId));

                    await _accounts.ResetAsync(uid);
                    return Ok(UseCouponResponse(1, req.packetId, [ItemReward(2)]));
                }

            case 'S':
                {
                    if (code.Length < 4 || code[..4] != "SYNC")
                        return Ok(UseCouponResponse(2, req.packetId));
                    if (!_config.Command.AllowSyncCommand)
                        return Ok(UseCouponResponse(2, req.packetId));

                    var flags = code.Length > 4 ? code[4..] : "";
                    var syncAll = flags.Length == 0;
                    var itemUpdate = syncAll || flags.Contains("ITEM", StringComparison.OrdinalIgnoreCase);
                    var egoUpdate = syncAll || flags.Contains("EGO", StringComparison.OrdinalIgnoreCase);
                    var personalityUpdate = syncAll || flags.Contains("PERSONALITY", StringComparison.OrdinalIgnoreCase);
                    var announcerUpdate = syncAll || flags.Contains("ANNOUNCER", StringComparison.OrdinalIgnoreCase);
                    var ticketUpdate = syncAll || flags.Contains("TICKET", StringComparison.OrdinalIgnoreCase);
                    var bannerUpdate = syncAll || flags.Contains("BANNER", StringComparison.OrdinalIgnoreCase);

                    var updated = new UpdatedFormat
                    {
                        personalityList = personalityUpdate
                            ? await _personalities.SyncNewAsync(uid, _staticData.PersonalityIds)
                            : null,
                        egoList = egoUpdate
                            ? await _egos.SyncNewAsync(uid, _staticData.EgoIds)
                            : null,
                        itemList = itemUpdate
                            ? await _items.SyncNewAsync(uid, _staticData.ItemIds)
                            : null,
                        announcer = announcerUpdate
                            ? await _announcers.SyncNewAsync(uid, _staticData.AnnouncerIds)
                            : null,
                        isUpdateUserBanner = bannerUpdate,
                    };

                    if (ticketUpdate)
                        await _tickets.SyncNewAsync(uid, _staticData.BorderLeftIds, _staticData.BorderRightIds, _staticData.EgoBgIds);
                    if (bannerUpdate)
                        await _banners.SyncNewAsync(uid, _staticData.UserBannerIds);

                    return new HttpResponseFormat<ResPacket_UseCoupon>
                    {
                        serverInfo = new ServerInfo { version = "product" },
                        state = "ok",
                        updated = updated,
                        result = new ResPacket_UseCoupon { state = 1, rewards = [ItemReward(2)] },
                        packetId = req.packetId,
                    };
                }

            case 'P':
                {
                    var args = ExtractInts(code, 3);
                    if (args[0] == 0)
                        return Ok(UseCouponResponse(2, req.packetId));

                    await _personalities.UpdateAsync(uid, args[0], args[1], args[2]);
                    return new HttpResponseFormat<ResPacket_UseCoupon>
                    {
                        serverInfo = new ServerInfo { version = "product" },
                        state = "ok",
                        updated = new UpdatedFormat
                        {
                            personalityList =
                            [
                                new PersonalityFormat
                            {
                                personality_id = args[0],
                                level = args[1],
                                gacksung = args[2],
                            },
                        ],
                        },
                        result = new ResPacket_UseCoupon
                        {
                            state = 1,
                            rewards =
                            [
                                new Element
                            {
                                type = ELEMENT_TYPE.PERSONALITY,
                                _type = (int)ELEMENT_TYPE.PERSONALITY,
                                id = args[0],
                                num = 1,
                            },
                        ],
                        },
                        packetId = req.packetId,
                    };
                }

            case 'E':
                {
                    var args = ExtractInts(code, 2);
                    if (args[0] == 0)
                        return Ok(UseCouponResponse(2, req.packetId));

                    await _egos.UpdateAsync(uid, args[0], args[1]);
                    return new HttpResponseFormat<ResPacket_UseCoupon>
                    {
                        serverInfo = new ServerInfo { version = "product" },
                        state = "ok",
                        updated = new UpdatedFormat
                        {
                            egoList =
                            [
                                new EgoFormat { ego_id = args[0], gacksung = args[1] },
                        ],
                        },
                        result = new ResPacket_UseCoupon
                        {
                            state = 1,
                            rewards =
                            [
                                new Element
                            {
                                type = ELEMENT_TYPE.EGO,
                                _type = (int)ELEMENT_TYPE.EGO,
                                id = args[0],
                                num = 1,
                            },
                        ],
                        },
                        packetId = req.packetId,
                    };
                }

            case 'I':
                {
                    var args = ExtractInts(code, 2);
                    if (args[0] == 0)
                        return Ok(UseCouponResponse(2, req.packetId));

                    await _items.UpdateAsync(uid, args[0], args[1]);
                    return new HttpResponseFormat<ResPacket_UseCoupon>
                    {
                        serverInfo = new ServerInfo { version = "product" },
                        state = "ok",
                        updated = new UpdatedFormat
                        {
                            itemList =
                            [
                                new ItemFormat { item_id = args[0], num = args[1] },
                        ],
                        },
                        result = new ResPacket_UseCoupon
                        {
                            state = 1,
                            rewards =
                            [
                                new Element
                            {
                                type = ELEMENT_TYPE.ITEM,
                                _type = (int)ELEMENT_TYPE.ITEM,
                                id = args[0],
                                num = args[1],
                            },
                        ],
                        },
                        packetId = req.packetId,
                    };
                }

            case 'U':
                {
                    var args = ExtractInts(code, 3);
                    if (args[0] == 0)
                        return Ok(UseCouponResponse(2, req.packetId));

                    await _users.UpdateAsync(args[0], args[1], stamina: args[2]);
                    return new HttpResponseFormat<ResPacket_UseCoupon>
                    {
                        serverInfo = new ServerInfo { version = "product" },
                        state = "ok",
                        updated = new UpdatedFormat
                        {
                            userInfo = await _users.GetAsync(args[0]),
                        },
                        result = new ResPacket_UseCoupon
                        {
                            state = 1,
                            rewards = [ItemReward(2)],
                        },
                        packetId = req.packetId,
                    };
                }

            default:
                return Ok(UseCouponResponse(2, req.packetId));
        }
    }

    private static HttpResponseFormat<ResPacket_UseCoupon> UseCouponResponse(
        int state, long packetId, List<Element>? rewards = null) => new()
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_UseCoupon { state = state, rewards = rewards ?? [] },
            packetId = packetId,
        };

    private static Element ItemReward(int id) => new()
    {
        type = ELEMENT_TYPE.ITEM,
        _type = (int)ELEMENT_TYPE.ITEM,
        id = id,
        num = 1,
    };

    private static int[] ExtractInts(string input, int expectedCount)
    {
        var result = new int[expectedCount];
        var matches = Regex.Matches(input, @"\d+");
        for (int i = 0; i < Math.Min(expectedCount, matches.Count); i++)
            result[i] = int.Parse(matches[i].Value);
        return result;
    }
}
