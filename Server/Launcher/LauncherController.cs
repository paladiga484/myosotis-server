using System.Diagnostics;
using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Server.Tweak;

namespace Server.Launcher;

public sealed record PlayRequest(string Mode);
public sealed record JoinRequest(string Host, string? Token);
public sealed record HostTest(string Host);
public sealed record HostingRequest(bool On, string? Address);

/// <summary>
/// The Windows app's window, served at GET /launcher: play here or on a friend's server, host
/// for friends, open the mod menu, quit. It starts programs, so it exists only when the package
/// turns it on, and like the mod menu it only answers the machine the server runs on.
/// </summary>
[ApiController]
[Route("launcher")]
[LocalOnly]
[LauncherEnabled]
public sealed class LauncherController(Config config, LauncherService launcher) : ControllerBase
{
    [HttpGet("")]
    public ContentResult Page() => Content(LauncherPage.Html, "text/html; charset=utf-8");

    /// <summary>The app's icon (icon.png beside Config.json), for the window and its taskbar button.</summary>
    [HttpGet("icon")]
    public IActionResult Icon()
    {
        var path = config.ResolvePath("icon.png");
        return System.IO.File.Exists(path) ? PhysicalFile(path, "image/png") : NotFound();
    }

    [HttpGet("api/state")]
    public object State()
    {
        launcher.Seen();
        var (mode, phase, message) = launcher.Now();
        var (host, tokenSet) = launcher.ReadJoin();
        return new
        {
            version = config.Server.Versions.FirstOrDefault() ?? "",
            game = new { running = LauncherService.GameRunning() },
            play = new { mode, phase, message },
            join = new { host, tokenSet },
            hosting = new { address = launcher.HostingAddress, candidates = LauncherService.TailscaleAddresses() },
        };
    }

    [HttpPost("api/play")]
    public IActionResult Play(PlayRequest req) =>
        launcher.Play(req.Mode) is { } error ? Fail(error) : Ok();

    [HttpPost("api/cancel")]
    public IActionResult Cancel()
    {
        launcher.Cancel();
        return Ok();
    }

    [HttpPost("api/join")]
    public IActionResult Join(JoinRequest req) =>
        launcher.WriteJoin(req.Host, req.Token) is { } error ? Fail(error) : Ok();

    [HttpPost("api/join/test")]
    public async Task<object> Test(HostTest req)
    {
        var (ok, versions, error) = await launcher.TestAsync(req.Host);
        return new { ok, versions, error };
    }

    [HttpPost("api/hosting")]
    public IActionResult Hosting(HostingRequest req)
    {
        if (launcher.SetHosting(req.On, req.Address) is { } error)
            return Fail(error);
        return launcher.Restart() is { } note ? Ok(new { note }) : Ok(new { restarting = true });
    }

    [HttpPost("api/logs")]
    public IActionResult Logs()
    {
        if (!OperatingSystem.IsWindows())
            return Fail("Opening folders is for the Windows package.");
        var logs = config.ResolvePath("logs");
        Directory.CreateDirectory(logs);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logs}\"") { UseShellExecute = false });
        return Ok();
    }

    [HttpPost("api/quit")]
    public IActionResult Quit()
    {
        if (LauncherService.GameRunning())
            return Fail("Limbus is still running and needs the server. Close the game first.", 409);
        launcher.Cancel();
        launcher.Stop();
        return Ok();
    }

    private ObjectResult Fail(string why, int statusCode = 400) => StatusCode(statusCode, new { error = why });
}

/// <summary>404 unless the friend package switched the launcher on.</summary>
public sealed class LauncherEnabledAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.RequestServices.GetRequiredService<Config>().Launcher.Enabled)
            context.Result = new NotFoundResult();
    }
}
