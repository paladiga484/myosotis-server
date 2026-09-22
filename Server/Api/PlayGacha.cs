using Microsoft.AspNetCore.Mvc;
using Resource;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Duplicate pulls convert to that identity's Egoshards, as they do in the real game.
    /// Note the "Egoshards" entry (item 5) in the localisation files is NOT an inventory item -
    /// it has no record in the item static data - so duplicates pay out in the per-sinner,
    /// per-season shard instead.
    /// </summary>
    private const int DuplicateShardValue = 100;

    [HttpPost("PlayGacha")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_PlayGacha>>> PlayGacha(
        [FromBody] HttpRequestFormat<ReqPacket_PlayGacha> req)
    {
        var uid = HttpContext.GetUid();
        var gachaId = _staticData.ResolveGachaId(req.parameters.gachaId);
        if (gachaId != req.parameters.gachaId)
            _logger.LogWarning(
                "Gacha {Requested} is newer than the static data dump; rolling banner {Used} instead",
                req.parameters.gachaId, gachaId);

        var entries = _staticData.GetGacha(gachaId);
        if (entries.Count == 0)
        {
            _logger.LogWarning("PlayGacha: no banner data at all for {GachaId}", req.parameters.gachaId);
            return EmptyGachaResult(req.packetId);
        }

        GachaPayment? payment = null;
        foreach (var entry in entries)
        {
            payment = entry.Payments.FirstOrDefault(p => p.PaymentId == req.parameters.paymentId);
            if (payment is not null)
                break;
        }

        if (payment is null || payment.Count <= 0)
        {
            _logger.LogWarning(
                "PlayGacha gacha {GachaId}: no payment {PaymentId}; known: {Known}",
                gachaId, req.parameters.paymentId,
                string.Join(", ", entries.SelectMany(e => e.Payments).Select(p => p.PaymentId)));
            return EmptyGachaResult(req.packetId);
        }

        var pullCount = payment.Count;

        // Charge for the pull. Without this, currency never moves and the client's own count
        // drifts away from the server's on the next data load.
        if (payment.RequiredItemId > 0 && payment.RequiredNum > 0)
        {
            var items = await _items.ListAsync(uid);
            var held = items.FirstOrDefault(i => i.item_id == payment.RequiredItemId)?.num ?? 0;
            if (held < payment.RequiredNum)
            {
                _logger.LogInformation(
                    "PlayGacha refused for uid {Uid}: needs {Need} of item {Item}, holds {Held}",
                    uid, payment.RequiredNum, payment.RequiredItemId, held);
                return EmptyGachaResult(req.packetId);
            }

            await _items.UpdateAsync(uid, payment.RequiredItemId, held - payment.RequiredNum);
        }

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

        // Roll each pull independently against the rate table, so a ten-pull can and does repeat
        // an identity - the old version drew a distinct set, which made dupes impossible.
        var pulled = new List<int>();
        for (int i = 0; i < pullCount; i++)
        {
            var roll = Random.Shared.NextDouble();
            var cumulative = 0.0;
            foreach (var (group, probability) in chance)
            {
                if (probability <= 0)
                    continue;
                cumulative += probability;
                if (roll > cumulative)
                    continue;

                var pool = idsByGroup[group].Distinct().ToList();
                if (pool.Count > 0)
                    pulled.Add(pool[Random.Shared.Next(pool.Count)]);
                break;
            }
        }

        // Fall back to any content if the rate table left a pull unfilled.
        var anyPool = idsByGroup.Values.SelectMany(x => x).Distinct().ToList();
        while (pulled.Count < pullCount && anyPool.Count > 0)
            pulled.Add(anyPool[Random.Shared.Next(anyPool.Count)]);

        // Actually hand them over. Previously the result was cosmetic: the animation played and
        // the account gained nothing.
        var ownedIds = await _personalities.GetIdsAsync(uid);
        var newIds = new List<int>();
        var duplicateIds = new List<int>();
        foreach (var id in pulled)
        {
            if (ownedIds.Add(id))
                newIds.Add(id);
            else
                duplicateIds.Add(id);
        }

        if (newIds.Count > 0)
            await _personalities.SyncNewAsync(uid, ownedIds);

        if (duplicateIds.Count > 0)
        {
            var items = await _items.ListAsync(uid);
            foreach (var group in duplicateIds.GroupBy(_staticData.EgoshardItemFor))
            {
                if (group.Key == 0)
                {
                    _logger.LogWarning(
                        "No Egoshard item for duplicate identities {Ids}; they paid out nothing",
                        string.Join(", ", group.Distinct()));
                    continue;
                }

                var held = items.FirstOrDefault(i => i.item_id == group.Key)?.num ?? 0;
                await _items.UpdateAsync(uid, group.Key, held + group.Count() * DuplicateShardValue);
            }
        }

        _logger.LogInformation(
            "PlayGacha uid {Uid} gacha {GachaId}: {Count} pulls, {New} new, {Dupes} duplicates "
            + "(cost {Num} of item {Item})",
            uid, gachaId, pullCount, newIds.Count, duplicateIds.Count,
            payment.RequiredNum, payment.RequiredItemId);

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
}
