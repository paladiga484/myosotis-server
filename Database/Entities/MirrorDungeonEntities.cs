namespace Database.Entities;

/// <summary>
/// One in-progress or finished Mirror Dungeon run per user.
///
/// The protocol round-trips nearly complete state on every request -
/// <c>MirrorDungeonCurrentInfoFormat</c> alone carries 30+ fields of data the client owns
/// (unit list, gift pools, shop, missions, starlight, constraints...). Nothing server-side
/// queries inside any of it, so it is stored as one JSON blob with only the scalars that are
/// actually filtered on promoted to columns. If something later needs to query inside the
/// blob, that field gets its own column then.
/// </summary>
public sealed class MirrorDungeonSave
{
    public long Uid { get; set; }
    public int DungeonId { get; set; }
    public int Idx { get; set; }
    public int IsEnded { get; set; }

    /// <summary>Serialised <c>MirrorDungeonSaveInfoFormat</c>.</summary>
    public string SaveJson { get; set; } = "";

    public long UpdatedAt { get; set; }
}
