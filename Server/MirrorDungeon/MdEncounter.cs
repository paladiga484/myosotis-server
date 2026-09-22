namespace Server.MirrorDungeon;

/// <summary>
/// Integer encounter types for <c>RandomDungeonMapNodeFormatForMapFormat.e</c>.
///
/// THIS MAPPING IS NOT YET CONFIRMED. The static data names encounter types as strings
/// ("BATTLE", "AB_BATTLE", "HARD_BATTLE", "HARD_AB_BATTLE", "BOSS", "EVENT") but the wire
/// format sends an int, and the enum lives in the client. It is not in the dumped types, and
/// the game's global-metadata.dat is AppSealing-encrypted so the names cannot be read out of
/// it either.
///
/// One value IS known from the static data: every floor of every dungeon-07 variant carries
/// <c>fixedEncounters: [{ targetSector: -2, e: 10, eid: 0 }]</c> - the node one step before the
/// boss, which in game is the rest stop / shop. So 10 is REST.
///
/// The rest below is inference. <see cref="MapGenerator"/> has a probe mode that lays one node
/// out per candidate value so the in-game icons identify them in a single run; correct these
/// constants from what that shows and everything downstream follows.
/// </summary>
public static class MdEncounter
{
    public const int None = 0;
    public const int Battle = 1;
    public const int HardBattle = 2;
    public const int AbBattle = 3;
    public const int HardAbBattle = 4;
    public const int Boss = 5;
    public const int Event = 6;

    /// <summary>Confirmed from static data <c>fixedEncounters</c>.</summary>
    public const int Rest = 10;

    /// <summary>Values the probe walks, to be identified by their rendered icon.</summary>
    public static readonly int[] ProbeRange = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
}
