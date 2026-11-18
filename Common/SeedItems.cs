namespace Common;

/// <summary>
/// What a seeded account gets in its inventory, per item, instead of a flat "N of all 196".
///
/// A blanket grant fills the bags with hundreds of every event token and crate, which is noise.
/// This gives generous amounts of the things you actually spend - pull currency, thread, shards,
/// crates - and nothing of the rest.
///
/// Item ids are from the static data (names via Localize/en/EN_Items*.json):
///   1 Paid Lunacy · 2 Free Lunacy · 3 Lunacy · 5 Egoshards · 11 Enkephalin Module
///   101 Extraction Ticket · 102 Decaextraction · 103-110 season 3* guarantee decas
///   201 Thread · 211-214 ID training · 221-226 level boost · 231-238 takeoff modules
///   301 Enkephalin Box · 401 Thread Crate · 501-503/601/703 nominable tickets
///   1101-1107 season Egoshard Crates · 1201-1207 nominable Egoshard Crates
///   10101-11207 per-sinner egoshards
/// Note: "Egoshards" (5) and the selected-sinner shards (10001-10007) appear in the localisation
/// files but have NO record in the item static data, so they cannot be held. They are left out.
///   12004-12007 universal uptie/threadspin shards · 9900 Threadskein · 20043 Severed Thread
/// </summary>
public static class SeedItems
{
    /// <summary>Exact amounts for single ids.</summary>
    private static readonly Dictionary<int, int> Exact = new()
    {
        [1] = 5_000,        // Paid Lunacy - gacha 1 payment 0 wants this
        [2] = 5_000,        // Free Lunacy
        [3] = 200_000,      // Lunacy: ~150 ten-pulls at 1300 each
        [11] = 1_000,       // Enkephalin Module
        [101] = 500,        // Extraction Ticket
        [102] = 500,        // Decaextraction Ticket
        [201] = 50_000,     // Thread
        [301] = 200,        // Enkephalin Box
        [401] = 200,        // Thread Crate
        [601] = 50,         // Nominable S1 Battle Pass E.G.O Ticket
        [703] = 50,         // 3rd Anniversary Nominable Identity Ticket
        [9900] = 5_000,     // Threadskein
        [20043] = 5_000,    // Severed Thread
    };

    /// <summary>Inclusive id ranges that all get the same amount.</summary>
    private static readonly (int From, int To, int Count)[] Ranges =
    [
        (103, 110, 100),      // season 3* guarantee decaextraction tickets
        (211, 238, 200),      // identity training, level boost, takeoff modules
        (501, 503, 50),       // nominable identity tickets
        (1101, 1107, 500),    // season Egoshard Crates
        (1201, 1207, 500),    // nominable Egoshard Crates
        (10101, 11207, 500),  // per-sinner egoshards
        (12004, 12007, 500),  // universal uptie / threadspinning shards
    ];

    /// <summary>How many of <paramref name="itemId"/> a curated account starts with. 0 = none.</summary>
    public static int CuratedCount(int itemId)
    {
        if (Exact.TryGetValue(itemId, out var n))
            return n;

        foreach (var (from, to, count) in Ranges)
            if (itemId >= from && itemId <= to)
                return count;

        return 0;
    }
}
