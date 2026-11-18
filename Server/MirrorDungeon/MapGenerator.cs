using Microsoft.Extensions.Logging;
using Resource;
using Types.Server;

namespace Server.MirrorDungeon;

/// <summary>
/// Builds one floor's node graph from the dungeon's floor weights and the chosen theme floor's
/// encounter pools.
///
/// Shape comes from the theme floor's <c>CREATE_EMPTY_MD4_FLOOR</c> step: (steps, width, boss).
/// A floor is <c>steps</c> encounter sectors, then a rest sector, then the boss - so the common
/// (4, 3, 1) theme gives six stages, which is what the game shows. The rest stop is its own
/// sector rather than an overwrite of the last encounter sector; getting that wrong is what made
/// floors come out one stage short.
///
/// Every node links to every node in the next sector, which is the simplest graph the client can
/// walk - if it wants a sparser fan-out, that is a change to <see cref="Link"/> alone.
/// </summary>
public sealed class MapGenerator(MirrorDungeonData data, Common.Config config, ILogger<MapGenerator> logger)
{
    /// <summary>
    /// Generate the map for one floor.
    /// </summary>
    /// <param name="probeEnum">
    /// Diagnostic mode: instead of drawing encounter types from the configured percentages, lay
    /// out one node per candidate value in <see cref="MdEncounter.ProbeRange"/> so the icons the
    /// client renders identify the unknown encounter enum. Logs the legend it used.
    /// </param>
    public RandomDungeonMapFormat Generate(
        int dungeonId, int typeIndex, int floor, int themeFloorId, int seed, bool probeEnum = false)
    {
        var dungeon = data.GetDungeon(dungeonId, typeIndex);
        var theme = data.GetThemeFloor(themeFloorId);
        if (dungeon is null || theme is null)
        {
            logger.LogWarning(
                "Cannot generate map: dungeon {DungeonId} present={HasDungeon}, theme {ThemeId} present={HasTheme}",
                dungeonId, dungeon is not null, themeFloorId, theme is not null);
            return new RandomDungeonMapFormat();
        }

        var (themeSteps, width, bossSteps) = theme.Shape;

        // Theme shapes are 3, 4 or 5 encounter sectors, which makes floors uneven (6/5/5/5/6 in
        // game). StagesPerFloor forces every floor to the same total; 0 keeps the theme's own.
        var steps = config.MirrorDungeon.StagesPerFloor > 0
            ? Math.Max(1, config.MirrorDungeon.StagesPerFloor - 1 - bossSteps)
            : themeSteps;
        // `count` is the floor index, not a node count. Extreme has 15 floor entries, infinite
        // 10, normal and hard 5 - so clamp rather than assume the floor exists.
        var floorConfig = dungeon.floors.FirstOrDefault(f => f.count == floor)
                          ?? dungeon.floors.LastOrDefault()
                          ?? new MdFloorConfig();

        var rng = new Random(seed);
        var map = new RandomDungeonMapFormat();
        var previousStep = new List<RandomDungeonMapNodeFormatForMapFormat>();
        var probeIndex = 0;

        // encounter sectors + one rest sector + the boss sector(s)
        var totalSectors = steps + 1 + bossSteps;

        for (int step = 0; step < totalSectors; step++)
        {
            bool isBossStep = step >= steps + 1;
            bool isRestStep = step == steps;
            // The boss and the rest stop are always alone; the first sector is a single entry
            // point so the run has one start. Everything between fans out.
            int nodeCount = isBossStep || isRestStep || step == 0 ? 1 : rng.Next(2, width + 1);

            var currentStep = new List<RandomDungeonMapNodeFormatForMapFormat>();
            for (int i = 0; i < nodeCount; i++)
            {
                var node = new RandomDungeonMapNodeFormatForMapFormat
                {
                    f = floor,
                    s = step,
                    nid = NodeId(floor, step, i),
                };

                if (isBossStep)
                {
                    node.e = MdEncounter.Boss;
                    node.eid = Pick(theme.mapGenOption.bossPool, rng);
                }
                else if (isRestStep)
                {
                    // Overwritten below by fixedEncounters, which every dungeon-07 variant sets
                    // to {targetSector: -2, e: 10, eid: 0} - the rest stop before the boss.
                    node.e = MdEncounter.Rest;
                    node.eid = 0;
                }
                else if (probeEnum)
                {
                    node.e = MdEncounter.ProbeRange[probeIndex++ % MdEncounter.ProbeRange.Length];
                    node.eid = Pick(theme.mapGenOption.battlePool, rng);
                    logger.LogInformation(
                        "  probe: floor {Floor} step {Step} node {Nid} -> e={E}",
                        floor, step, node.nid, node.e);
                }
                else
                {
                    (node.e, node.eid) = RollEncounter(floorConfig, theme.mapGenOption, rng);
                }

                currentStep.Add(node);
            }

            ApplyFixedEncounters(floorConfig, currentStep, step, totalSectors);
            Link(previousStep, currentStep);

            map.ns.AddRange(currentStep);
            previousStep = currentStep;
        }

        logger.LogDebug(
            "Generated floor {Floor} of dungeon {DungeonId}/{TypeIndex} (theme {ThemeId}): {Nodes} nodes over {Steps} steps{Probe}",
            floor, dungeonId, typeIndex, themeFloorId, map.ns.Count, totalSectors,
            probeEnum ? " [ENUM PROBE]" : "");

        return map;
    }

    /// <summary>
    /// Build the whole run's map, every floor at once.
    ///
    /// The first pass generated floor 0 only, which is why the client showed a single stage set
    /// and could not advance. Node ids already carry the floor, so all floors coexist in the one
    /// <c>ns</c> list the format expects.
    /// </summary>
    public RandomDungeonMapFormat GenerateRun(
        int dungeonId,
        int typeIndex,
        IReadOnlyList<RandomDungeonMapThemeFormat> themesPerFloor,
        int seed,
        bool probeEnum = false)
    {
        var run = new RandomDungeonMapFormat();

        for (int floor = 0; floor < themesPerFloor.Count; floor++)
        {
            // Vary the seed per floor or every floor comes out identical.
            var floorMap = Generate(
                dungeonId, typeIndex, floor, themesPerFloor[floor].tfid,
                unchecked(seed + floor * 7919), probeEnum);
            run.ns.AddRange(floorMap.ns);
        }

        logger.LogInformation(
            "Generated run for dungeon {DungeonId}/{TypeIndex}: {Floors} floors, {Nodes} nodes total{Probe}",
            dungeonId, typeIndex, themesPerFloor.Count, run.ns.Count,
            probeEnum ? " [ENUM PROBE]" : "");

        return run;
    }

    /// <summary>Unique within a run: floor, step and index are all recoverable from it.</summary>
    private static int NodeId(int floor, int step, int index) =>
        (floor + 1) * 1000 + step * 10 + index;

    /// <summary>Draw an encounter type using the floor's configured percentages.</summary>
    private static (int E, int Eid) RollEncounter(MdFloorConfig floor, MdMapGenOption pools, Random rng)
    {
        double roll = rng.NextDouble();
        double threshold = floor.eventNodePercentage;

        if (roll < threshold && pools.eventPool.Count > 0)
            return (MdEncounter.Event, Pick(pools.eventPool, rng));

        threshold += floor.hardAbBattlePercentage;
        if (roll < threshold && pools.hardAbBattlePool.Count > 0)
            return (MdEncounter.HardAbBattle, Pick(pools.hardAbBattlePool, rng));

        threshold += floor.abnormalityBattlePercentage;
        if (roll < threshold && pools.abBattlePool.Count > 0)
            return (MdEncounter.AbBattle, Pick(pools.abBattlePool, rng));

        threshold += floor.hardBattlePercentage;
        if (roll < threshold && pools.hardBattlePool.Count > 0)
            return (MdEncounter.HardBattle, Pick(pools.hardBattlePool, rng));

        return (MdEncounter.Battle, Pick(pools.battlePool, rng));
    }

    /// <summary>
    /// Honour <c>fixedEncounters</c>. A negative <c>targetSector</c> counts back from the end,
    /// so -2 is the step before the last - the rest stop before the boss.
    /// </summary>
    private static void ApplyFixedEncounters(
        MdFloorConfig floor,
        List<RandomDungeonMapNodeFormatForMapFormat> step,
        int stepIndex,
        int totalSteps)
    {
        foreach (var fixedEncounter in floor.fixedEncounters)
        {
            int target = fixedEncounter.targetSector < 0
                ? totalSteps + fixedEncounter.targetSector
                : fixedEncounter.targetSector;

            if (target != stepIndex)
                continue;

            foreach (var node in step)
            {
                node.e = fixedEncounter.e;
                node.eid = fixedEncounter.eid;
            }
        }
    }

    /// <summary>Every node in the earlier step reaches every node in the next.</summary>
    private static void Link(
        List<RandomDungeonMapNodeFormatForMapFormat> from,
        List<RandomDungeonMapNodeFormatForMapFormat> to)
    {
        foreach (var node in from)
            node.nnids = to.Select(n => n.nid).ToList();
    }

    private static int Pick(List<int> pool, Random rng) =>
        pool.Count == 0 ? 0 : pool[rng.Next(pool.Count)];
}
