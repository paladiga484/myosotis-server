using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Start a run: pick the theme floors, generate every floor's map, persist it and hand the
    /// whole save back to the client.
    ///
    /// <c>idx</c> on the request is the difficulty: all four Mirror Dungeon variants share
    /// dungeon id 7 and differ only by <c>typeIndex</c> — 0 normal and 1 hard are 5 floors,
    /// 2 infinite is 10, 3 extreme is 15.
    /// </summary>
    [HttpPost("EnterMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_EnterMirrorDungeon>>> EnterMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_EnterMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var dungeonId = req.parameters.dungeonid;
        var typeIndex = req.parameters.idx;

        var dungeon = _mirrorData.GetDungeon(dungeonId, typeIndex);
        if (dungeon is null)
        {
            _logger.LogWarning(
                "EnterMirrorDungeon for unknown dungeon {DungeonId} type {TypeIndex}; known: {Known}",
                dungeonId, typeIndex,
                string.Join(", ", _mirrorData.Dungeons.Keys.Select(k => $"{k.Id}/{k.TypeIndex}")));
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        // One seed per run so the map is stable across reconnects; the run is identified by the
        // time it started, which also gives us `startdate` for free.
        var startedAt = Common.TimeUtil.UnixNow();
        var seed = unchecked((int)(uid * 31 + startedAt));

        // Every floor the variant defines, not just up to winFloorIdx: infinite and extreme carry
        // more floor entries than winFloorIdx and the client walks all of them.
        int floorCount = Math.Max(dungeon.floors.Count, dungeon.winFloorIdx + 1);
        var chosenThemes = _themePicker.PickChosen(floorCount, seed);
        var themePools = _themePicker.PickPools(floorCount, seed);

        var map = _mapGenerator.GenerateRun(
            dungeonId, typeIndex, chosenThemes, seed,
            probeEnum: _config.MirrorDungeon.ProbeEncounterEnum);

        var save = new MirrorDungeonSaveInfoFormat
        {
            dungeonId = dungeonId,
            idx = typeIndex,
            isEndDungeon = 0,
            isReset = 0,
            version = 1,
            startdate = DateTimeOffset.FromUnixTimeSeconds(startedAt).ToString("yyyy-MM-dd HH:mm:ss"),
            dungeonMap = map,
            currentInfo = new MirrorDungeonCurrentInfoFormat
            {
                // -1 means "not inside a node yet": the player is looking at the map.
                eid = -1,
                cost = dungeon.startCost,
                usedcost = 0,
                tfs = chosenThemes,
                tfps = themePools,
                tfpsCreated = 1,
                rentalid = req.parameters.rentalFormationId,
            },
        };

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        _logger.LogInformation(
            "EnterMirrorDungeon uid={Uid} dungeon={DungeonId} type={TypeIndex}: {Floors} floors, "
            + "{Nodes} nodes, themes [{Themes}]",
            uid, dungeonId, typeIndex, floorCount, map.ns.Count,
            string.Join(", ", chosenThemes.Select(t => t.tfid)));

        return new HttpResponseFormat<ResPacket_EnterMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_EnterMirrorDungeon
            {
                saveInfo = save,
                recentCharacterList = [],
            },
            packetId = req.packetId,
        };
    }
}
