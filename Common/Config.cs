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

public sealed class LoggingConfig
{
    public string MinimumLevel { get; set; } = "Information";
    public bool LogBodies { get; set; } = false;
}
