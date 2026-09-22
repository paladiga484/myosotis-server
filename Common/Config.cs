using System.Text.Json;

namespace Common;

public sealed class Config
{
    public ServerConfig Server { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public StaticDataConfig StaticData { get; set; } = new();
    public AuthConfig Auth { get; set; } = new();
    public CommandConfig Command { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public SeedConfig Seed { get; set; } = new();
    public MirrorDungeonConfig MirrorDungeon { get; set; } = new();

    public string Root { get; set; } = "";

    public string ResolvePath(string relative) => Path.GetFullPath(relative, Root);

    public static Config Load()
    {
        string? configFile = Environment.GetEnvironmentVariable("MYOSOTIS_CONFIG");
        if (!string.IsNullOrEmpty(configFile))
        {
            configFile = Path.GetFullPath(configFile);
            if (!File.Exists(configFile))
                throw new FileNotFoundException($"MYOSOTIS_CONFIG points at '{configFile}', which does not exist.");
        }
        else
        {
            var searched = new List<string>();
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "Config.json");
                searched.Add(candidate);
                if (File.Exists(candidate))
                {
                    configFile = candidate;
                    break;
                }

                dir = dir.Parent;
            }

            if (configFile is null)
                throw new FileNotFoundException(
                    "Config.json not found. Searched: " + string.Join(", ", searched));
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
        };

        var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile), options) ?? new Config();
        config.Root = Path.GetDirectoryName(Path.GetFullPath(configFile))!;
        return config;
    }
}

public sealed class ServerConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3000;
    public string[] Versions { get; set; } = ["1.111.1"];
}

public sealed class DatabaseConfig
{
    public string Path { get; set; } = "myosotis.db";
}

public sealed class StaticDataConfig
{
    public string Path { get; set; } = "Resource/LimbusStaticData/StaticData/static-data";
}

public sealed class AuthConfig
{
    public bool RequireToken { get; set; } = true;
}

public sealed class CommandConfig
{
    public bool AllowSyncCommand { get; set; } = true;
    public bool AllowResetDbCommand { get; set; } = true;
}

/// <summary>What a brand-new account starts with. Defaults reproduce the old behaviour.</summary>
public sealed class SeedConfig
{
    /// <summary>false = new accounts start empty and you grant things in the tweaker.</summary>
    public bool GrantEverything { get; set; } = true;

    /// <summary>
    /// What a new (or reset) account is given. Supersedes <see cref="GrantEverything"/>.
    ///
    ///   everything  the full 184 identities / 115 E.G.O. / every item  (the old behaviour)
    ///   balanced    a sane roster: all 12 base identities, a slice of the rest, some E.G.O.
    ///   starter     the 12 base "LCB Sinner" identities only, no E.G.O.
    ///   empty       nothing at all
    ///
    /// Every profile still creates the account scaffolding - announcers, profile row, banners,
    /// formations, railway save - because login needs it. An account with no profile row gives
    /// the client nothing to load.
    /// </summary>
    public string Profile { get; set; } = "everything";

    /// <summary>Fraction of the non-base identities granted under the balanced profile.</summary>
    public int BalancedRank2Every { get; set; } = 2;
    public int BalancedRank3Every { get; set; } = 4;
    public int BalancedEgoEvery { get; set; } = 3;
    public int UserLevel { get; set; } = SeedValues.UserLevel;
    public int Stamina { get; set; } = SeedValues.UserStamina;
    public int PersonalityLevel { get; set; } = SeedValues.PersonalityLevel;
    public int PersonalityUptie { get; set; } = SeedValues.PersonalityGacksung;
    public int EgoUptie { get; set; } = SeedValues.EgoGacksung;
    public int ItemCount { get; set; } = SeedValues.ItemCount;

    /// <summary>
    /// How the inventory is seeded.
    ///   all       <see cref="ItemCount"/> of every one of the 196 items (fills the bags with noise)
    ///   curated   generous amounts of what you spend, nothing of the rest - see <see cref="SeedItems"/>
    ///   none      no items
    /// </summary>
    public string ItemProfile { get; set; } = "all";
}

public sealed class LoggingConfig
{
    public string MinimumLevel { get; set; } = "Information";
    public bool LogBodies { get; set; } = false;
}

public sealed class MirrorDungeonConfig
{
    /// <summary>Dungeon id the client uses for the current Mirror Dungeon.</summary>
    public int DungeonId { get; set; } = 7;

    /// <summary>
    /// Diagnostic: lay out one node per candidate encounter-type value instead of a normal map,
    /// so the icons the client draws identify the unknown <c>e</c> enum. Leave off for play.
    /// </summary>
    public bool ProbeEncounterEnum { get; set; }

    /// <summary>
    /// Stages per floor, counting the rest stop and the boss. 6 matches what the game shows for
    /// a standard run. Set to 0 to use each theme floor's own shape instead, which is what the
    /// static data specifies and which produces uneven floors (6/5/5/5/6).
    /// </summary>
    public int StagesPerFloor { get; set; } = 6;
}
