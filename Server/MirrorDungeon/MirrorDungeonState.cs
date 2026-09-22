using Types.Server;

namespace Server.MirrorDungeon;

/// <summary>
/// Small shared mutations on a run's saved state, so the endpoints stay thin.
///
/// The server is a state-keeper: the client owns combat and sends results back. These helpers
/// exist to keep "what a node result means for the save" in one place rather than duplicated
/// across the node endpoints.
/// </summary>
public static class MirrorDungeonState
{
    /// <summary>Record the party chosen for the run.</summary>
    public static void ApplyFormation(
        MirrorDungeonSaveInfoFormat save, IReadOnlyList<MirrorDungeonFormationFormat> formation)
    {
        var info = save.currentInfo;

        // `dul` is the in-dungeon unit list and is positional - one entry per formation slot.
        info.dul = formation.Select(_ => new MirrorDungeonSaveUnitInfoFormat()).ToList();

        // `prevdul` keeps the identity behind each slot, which the client uses to restore a run.
        info.prevdul = formation
            .Select(f => new MirrorDungeonPrevUnitInfoFormat
            {
                pid = f.nextPersonalityId != 0 ? f.nextPersonalityId : f.pervPersonalityId,
            })
            .ToList();

        info.upids = info.prevdul.Select(u => u.pid).Where(pid => pid != 0).ToList();
    }

    /// <summary>Which floor a node id belongs to, using the encoding the generator applies.</summary>
    public static int FloorOf(MirrorDungeonSaveInfoFormat save, int nodeId) =>
        save.dungeonMap.ns.FirstOrDefault(n => n.nid == nodeId)?.f ?? 0;

    /// <summary>True when the node is that floor's terminal boss.</summary>
    public static bool IsBossNode(MirrorDungeonSaveInfoFormat save, int nodeId)
    {
        var node = save.dungeonMap.ns.FirstOrDefault(n => n.nid == nodeId);
        return node is not null && node.nnids.Count == 0;
    }

    /// <summary>Mark a floor cleared, once.</summary>
    public static void MarkFloorCleared(MirrorDungeonSaveInfoFormat save, int floor)
    {
        if (save.currentInfo.cfs.Any(c => c.floor == floor))
            return;

        save.currentInfo.cfs.Add(new MirrorDungeonClearedFloorFormat
        {
            floor = floor,
            difficulty = save.idx,
        });
    }
}
