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
        var items = await _items.ListAsync(uid);
        var changedItems = new Dictionary<int, int>();

        // Charge for the pull. Without this, currency never moves and the client's own count
        // drifts away from the server's on the next data load.
        if (payment.RequiredItemId > 0 && payment.RequiredNum > 0)
        {
            var held = items.FirstOrDefault(i => i.item_id == payment.RequiredItemId)?.num ?? 0;
            if (held < payment.RequiredNum)
            {
                _logger.LogInformation(
                    "PlayGacha refused for uid {Uid}: needs {Need} of item {Item}, holds {Held}",
                    uid, payment.RequiredNum, payment.RequiredItemId, held);
                return EmptyGachaResult(req.packetId);
            }

            changedItems[payment.RequiredItemId] = held - payment.RequiredNum;
        }

        // Rates come from the banner itself: each content group carries its weight out of 10000
        // per draw case. The old hard-coded table gave the featured 000 5% against 2.5% for every
        // other 000 combined, so the pickup (Dimension Shredder Yi Sang on fallback banner 288)
        // came up constantly.
        var ownedIds = await _personalities.GetIdsAsync(uid);
        var ownedEgos = await _egos.GetIdsAsync(uid);
        var contents = entries.SelectMany(e => e.Contents)
            .Where(c => c.ElementType is "PERSONALITY" or "EGO" && c.ElementIdList.Count > 0)
            .ToList();

        var pulled = new List<(ELEMENT_TYPE Type, int Id)>();
        for (int i = 0; i < pullCount; i++)
        {
            var tenth = pullCount >= 10 && i == pullCount - 1;
            // E.G.O. are never pulled twice; once the banner's are all owned, the game switches
            // to the COMPLETE_EGO_* weights, which move the E.G.O. share onto the identities.
            var egoPool = contents.Where(c => c.ElementType == "EGO")
                .SelectMany(c => c.ElementIdList).Where(id => !ownedEgos.Contains(id)).Distinct().ToList();
            var caseName = (egoPool.Count == 0 ? "COMPLETE_EGO_" : "") + (tenth ? "TENTH" : "DEFAULT");

            var weighted = contents
                .Where(c => c.ElementType != "EGO" || egoPool.Count > 0)
                .Select(c => (Content: c, Weight: Weight(c, caseName, tenth)))
                .Where(x => x.Weight > 0)
                .ToList();
            var total = weighted.Sum(x => x.Weight);
            if (total <= 0)
                break;

            var roll = Random.Shared.Next(total);
            var chosen = weighted[^1].Content;
            foreach (var (content, weight) in weighted)
            {
                if (roll < weight) { chosen = content; break; }
                roll -= weight;
            }

            if (chosen.ElementType == "EGO")
            {
                var ego = egoPool[Random.Shared.Next(egoPool.Count)];
                ownedEgos.Add(ego);
                pulled.Add((ELEMENT_TYPE.EGO, ego));
            }
            else
            {
                var pool = chosen.ElementIdList;
                pulled.Add((ELEMENT_TYPE.PERSONALITY, pool[Random.Shared.Next(pool.Count)]));
            }
        }

        // Hand them over, and tell the client what changed. Duplicates name their payout in
        // `ex`; leaving it empty is what broke the result screen (and its skip button) on an
        // account that already owns everything.
        var details = new List<GachaLogDetail>();
        var newPersonalities = new List<int>();
        var newEgos = new List<int>();
        var duplicates = 0;
        foreach (var (type, id) in pulled)
        {
            var detail = new GachaLogDetail { type = type, _type = (int)type, id = id };
            if (type == ELEMENT_TYPE.EGO)
                newEgos.Add(id);
            else if (ownedIds.Add(id))
                newPersonalities.Add(id);
            else
            {
                duplicates++;
                var shard = _staticData.EgoshardItemFor(id);
                if (shard == 0)
                    _logger.LogWarning("No Egoshard item for duplicate identity {Id}; it paid out nothing", id);
                else
                {
                    var held = changedItems.TryGetValue(shard, out var c)
                        ? c
                        : items.FirstOrDefault(x => x.item_id == shard)?.num ?? 0;
                    changedItems[shard] = held + DuplicateShardValue;
                    detail.ex = new Element
                    {
                        type = ELEMENT_TYPE.ITEM, _type = (int)ELEMENT_TYPE.ITEM,
                        id = shard, num = DuplicateShardValue,
                    };
                }
            }
            details.Add(detail);
        }

        foreach (var (itemId, num) in changedItems)
            await _items.UpdateAsync(uid, itemId, num);

        var personalityList = newPersonalities.Count > 0
            ? (await _personalities.SyncNewAsync(uid, ownedIds)).Where(p => newPersonalities.Contains(p.personality_id)).ToList()
            : null;
        var egoList = newEgos.Count > 0 ? await _egos.SyncNewAsync(uid, newEgos) : null;

        _logger.LogInformation(
            "PlayGacha uid {Uid} gacha {GachaId}: {Count} pulls, {New} new identities, {Egos} E.G.O., "
            + "{Dupes} duplicates (cost {Num} of item {Item}): {Ids}",
            uid, gachaId, pullCount, newPersonalities.Count, newEgos.Count, duplicates,
            payment.RequiredNum, payment.RequiredItemId, string.Join(",", pulled.Select(p => p.Id)));

        return new HttpResponseFormat<ResPacket_PlayGacha>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            updated = new UpdatedFormat
            {
                itemList = changedItems.Select(kv => new ItemFormat { item_id = kv.Key, num = kv.Value }).ToList(),
                personalityList = personalityList,
                egoList = egoList,
            },
            result = new ResPacket_PlayGacha { gachaLogDetails = details },
            packetId = req.packetId,
        };
    }

    /// <summary>A group's weight for this draw. Old banners without occupancy fall back to
    /// the game's published rates (0: 83%, 00: 12.8%, 000: 2.9% split with the pickup).</summary>
    private static int Weight(GachaContent c, string caseName, bool tenth)
    {
        if (c.Occupancy.Count > 0)
            return c.Occupancy.TryGetValue(caseName, out var w) ? w
                : !caseName.StartsWith("COMPLETE_EGO_") ? 0
                : c.Occupancy.GetValueOrDefault(caseName["COMPLETE_EGO_".Length..]);

        return c.GroupType switch
        {
            "1" => tenth ? 0 : 8300,
            "2" => tenth ? 9580 : 1280,
            "2_pickup" => 0,
            "3" or "3_pickup" => 145,
            "EGO" => 130,
            _ => 0,
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
