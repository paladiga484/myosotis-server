using Resource;
using Types.Server;

namespace Server.MirrorDungeon;

/// <summary>
/// Chooses which theme floor each floor of a run uses, and what alternatives were offered.
///
/// The real game offers the player a few theme floors per floor and they pick one. For the
/// spike this commits to a choice up front so a map can be generated; interactive selection is
/// <c>SelectThemeFloorMirrorDungeon</c> in the rest of Phase A.
/// </summary>
public sealed class ThemeFloorPicker(MirrorDungeonData data)
{
    private const int ChoicesPerFloor = 3;

    /// <summary>Theme floor chosen per floor, index 0..floorCount-1.</summary>
    public List<RandomDungeonMapThemeFormat> PickChosen(int floorCount, int seed)
    {
        var rng = new Random(seed);
        var candidates = Candidates();
        var chosen = new List<RandomDungeonMapThemeFormat>();

        for (int floor = 0; floor < floorCount; floor++)
        {
            if (candidates.Count == 0)
                break;

            var theme = candidates[rng.Next(candidates.Count)];
            chosen.Add(new RandomDungeonMapThemeFormat
            {
                f = floor,
                tid = theme.id,
                idx = floor,
                tfid = theme.id,
                egs = theme.egoGiftPool,
            });
        }

        return chosen;
    }

    /// <summary>The alternatives shown to the player per floor.</summary>
    public List<ThemeFloorPool> PickPools(int floorCount, int seed)
    {
        var rng = new Random(seed + 1);
        var candidates = Candidates();
        var pools = new List<ThemeFloorPool>();

        for (int floor = 0; floor < floorCount; floor++)
            for (int choice = 0; choice < ChoicesPerFloor && candidates.Count > 0; choice++)
            {
                var theme = candidates[rng.Next(candidates.Count)];
                pools.Add(new ThemeFloorPool
                {
                    idx = floor,
                    tfid = theme.id,
                    egs = theme.egoGiftPool,
                });
            }

        return pools;
    }

    /// <summary>
    /// Only theme floors that can actually produce a map: they need a boss and a battle pool,
    /// or the generator would emit nodes with encounter id 0.
    /// </summary>
    private List<MdThemeFloor> Candidates() =>
        data.ThemeFloors.Values
            .Where(t => t.mapGenOption.bossPool.Count > 0 && t.mapGenOption.battlePool.Count > 0)
            .OrderBy(t => t.id)
            .ToList();
}
