using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    [HttpPost("PlayGacha")]
    public ActionResult<HttpResponseFormat<ResPacket_PlayGacha>> PlayGacha(
        [FromBody] HttpRequestFormat<ReqPacket_PlayGacha> req)
    {
        var entries = _staticData.GetGacha(req.parameters.gachaId);
        if (entries.Count == 0)
            return EmptyGachaResult(req.packetId);

        var pullCount = 0;
        foreach (var entry in entries)
        {
            var payment = entry.Payments.FirstOrDefault(p => p.PaymentId == req.parameters.paymentId);
            if (payment is not null)
            {
                pullCount = payment.Count;
                break;
            }
        }

        if (pullCount <= 0)
            return EmptyGachaResult(req.packetId);

        var chance = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["1_pickup"] = 0.1,
            ["1"] = 0.8,
            ["2"] = 0.075,
            ["3"] = 0.025,
            ["3_pickup"] = 0.0,
            ["2_pickup"] = 0.0,
        };

        var featuredAdjusted = false;
        foreach (var entry in entries)
        {
            var groupTypes = entry.Contents
                .Where(c => c.ElementType == "PERSONALITY")
                .Select(c => c.GroupType)
                .ToHashSet();

            if (groupTypes.Contains("3_pickup") && !featuredAdjusted)
            {
                chance["3_pickup"] = 0.05;
                chance["1"] -= 0.05;
                featuredAdjusted = true;
            }
            else if (groupTypes.Contains("2_pickup") && !featuredAdjusted)
            {
                chance["2_pickup"] = 0.15;
                chance["1"] -= 0.15;
                featuredAdjusted = true;
            }
        }

        var idsByGroup = chance.Keys.ToDictionary(g => g, _ => new List<int>());
        foreach (var entry in entries)
        {
            foreach (var content in entry.Contents.Where(c => c.ElementType == "PERSONALITY" && idsByGroup.ContainsKey(c.GroupType)))
                idsByGroup[content.GroupType].AddRange(content.ElementIdList);
        }

        var pulled = new List<int>();
        var remaining = pullCount;
        foreach (var (group, probability) in chance)
        {
            if (probability <= 0)
                continue;
            var pool = idsByGroup[group].Distinct().ToList();
            if (pool.Count == 0)
                continue;

            var take = Math.Min((int)Math.Round(pullCount * probability), pool.Count);
            pulled.AddRange(Shuffle(pool).Take(take));
            remaining -= take;
        }

        if (remaining > 0)
        {
            var leftover = idsByGroup.Values.SelectMany(x => x).Distinct().Where(id => !pulled.Contains(id)).ToList();
            if (leftover.Count > 0)
                pulled.AddRange(Shuffle(leftover).Take(Math.Min(remaining, leftover.Count)));
        }

        pulled = Shuffle(pulled.Distinct().ToList()).Take(Math.Min(pullCount, pulled.Count)).ToList();

        return new HttpResponseFormat<ResPacket_PlayGacha>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_PlayGacha
            {
                gachaLogDetails = pulled.Select(id => new GachaLogDetail
                {
                    type = ELEMENT_TYPE.PERSONALITY,
                    _type = (int)ELEMENT_TYPE.PERSONALITY,
                    id = id,
                    ex = new Element(),
                    _origin = new Element(),
                }).ToList(),
            },
            packetId = req.packetId,
        };
    }

    private static HttpResponseFormat<ResPacket_PlayGacha> EmptyGachaResult(long packetId) => new()
    {
        serverInfo = new ServerInfo { version = "product" },
        state = "ok",
        result = new ResPacket_PlayGacha(),
        packetId = packetId,
    };

    private static List<int> Shuffle(List<int> items)
    {
        var list = items.ToList();
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}
