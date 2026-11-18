using Microsoft.AspNetCore.Mvc;
using Types.Server;

namespace Server.Api;

public partial class ApiController
{
    /// <summary>
    /// Commit the player's theme-floor choice for a floor. Without this the run cannot advance
    /// past the floor it started on.
    ///
    /// The map for every floor is already generated at entry, so this records the choice and, if
    /// the player picked something other than what was pre-rolled, regenerates that floor to match.
    /// </summary>
    [HttpPost("SelectThemeFloorMirrorDungeon")]
    public async Task<ActionResult<HttpResponseFormat<ResPacket_SelectThemeFloorMirrorDungeon>>> SelectThemeFloorMirrorDungeon(
        [FromBody] HttpRequestFormat<ReqPacket_SelectThemeFloorMirrorDungeon> req)
    {
        var uid = HttpContext.GetUid();
        var save = await _mirrorSaves.GetSaveAsync(uid);
        if (save is null)
        {
            _logger.LogWarning("SelectThemeFloorMirrorDungeon with no run in progress for uid {Uid}", uid);
            return StatusCode(StatusCodes.Status400BadRequest);
        }

        var floor = req.parameters.selectedIdx;
        var themeId = req.parameters.selectedThemeFoorId;   // the game's own spelling

        var existing = save.currentInfo.tfs.FirstOrDefault(t => t.f == floor);
        bool changed = existing is null || existing.tfid != themeId;

        if (existing is null)
        {
            var theme = _mirrorData.GetThemeFloor(themeId);
            save.currentInfo.tfs.Add(new RandomDungeonMapThemeFormat
            {
                f = floor,
                tid = themeId,
                idx = floor,
                tfid = themeId,
                egs = theme?.egoGiftPool ?? [],
            });
        }
        else if (changed)
        {
            existing.tid = themeId;
            existing.tfid = themeId;
            existing.egs = _mirrorData.GetThemeFloor(themeId)?.egoGiftPool ?? [];
        }

        if (changed)
        {
            // Re-roll just this floor so its encounters come from the theme actually chosen.
            var seed = unchecked(uid.GetHashCode() * 31 + floor * 7919 + themeId);
            var replacement = _mapGenerator.Generate(
                save.dungeonId, save.idx, floor, themeId, seed,
                probeEnum: _config.MirrorDungeon.ProbeEncounterEnum);

            save.dungeonMap.ns.RemoveAll(n => n.f == floor);
            save.dungeonMap.ns.AddRange(replacement.ns);

            _logger.LogInformation(
                "Theme floor for uid {Uid} floor {Floor} set to {ThemeId}: {Nodes} nodes regenerated",
                uid, floor, themeId, replacement.ns.Count);
        }

        if (!await _mirrorSaves.StoreSaveAsync(uid, save))
            return StatusCode(StatusCodes.Status500InternalServerError);

        return new HttpResponseFormat<ResPacket_SelectThemeFloorMirrorDungeon>
        {
            serverInfo = new ServerInfo { version = "product" },
            state = "ok",
            result = new ResPacket_SelectThemeFloorMirrorDungeon { saveInfo = save },
            packetId = req.packetId,
        };
    }
}
