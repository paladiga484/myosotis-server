using Types.Server;

namespace Server.Api;

/// <summary>
/// Story Dungeons (the Canto dungeon maps). The map is the static data's own (Resource/
/// StoryDungeonData); the server keeps the run - team, where the party stands, opened nodes,
/// gifts - and, per dungeon, the nodes ever cleared. Clearing pays nothing yet, like story stages.
/// </summary>
public partial class ApiController
{
    static HttpResponseFormat<T> StoryOk<T>(T result, long packetId) where T : new() => new()
    {
        serverInfo = new ServerInfo { version = "product" },
        state = "ok",
        result = result,
        packetId = packetId,
    };

    /// <summary>The run's map node as the client counts it: (floor, sector, node id).</summary>
    static DungeonMapNodeFormat StoryNode(int floor, int sector, int node) => new() { f = floor, s = sector, nid = node };
}
