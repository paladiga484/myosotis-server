namespace Database.Entities;

/// <summary>
/// A user's Story Dungeon state: the run in progress (if any) and, per dungeon, the nodes they
/// have cleared - which outlive the run. Like <see cref="MirrorDungeonSave"/>, the client owns
/// the shape of the save and nothing queries inside it, so it's JSON.
/// </summary>
public sealed class StoryDungeonSave
{
    public long Uid { get; set; }
    public int DungeonId { get; set; }

    /// <summary>Serialised <c>StoryDungeonSaveInfoFormat</c>; empty when no run is in progress.</summary>
    public string SaveJson { get; set; } = "";

    /// <summary>Serialised {dungeon id: [cleared node ids]}.</summary>
    public string RecordsJson { get; set; } = "";

    public long UpdatedAt { get; set; }
}
