using System.Text.Json;
using Common;
using Database;
using Database.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resource;

namespace Server.Tweak;

public sealed record PersonalityRow(int Id, string Sinner, string Title, int Rank, bool Owned, int Level, int Uptie, int Illust);
public sealed record EgoRow(int Id, string Name, bool Owned, int Uptie);
public sealed record ItemRow(int Id, int Num, string Name);
public sealed record CollectionState(string Key, string Label, List<int> All, List<int> Owned);
public sealed record UserRow(long Uid, string Credential, int Level, int Exp, int Stamina);
public sealed record AccountState(
    UserRow User,
    List<PersonalityRow> Personalities,
    List<EgoRow> Egos,
    List<ItemRow> Items,
    List<CollectionState> Collections);

public sealed record PersonalityEdit(int Id, bool Owned, int Level, int Uptie, int? Illust);
public sealed record EgoEdit(int Id, bool Owned, int Uptie);
public sealed record ItemEdit(int Id, int Num);
public sealed record UserEdit(int? Level, int? Exp, int? Stamina);
public sealed record ApplyRequest(
    UserEdit? User,
    List<PersonalityEdit>? Personalities,
    List<EgoEdit>? Egos,
    List<ItemEdit>? Items,
    int? ItemsSetAll,
    Dictionary<string, List<int>>? Collections);

public sealed record SeedDto(
    bool GrantEverything, int UserLevel, int Stamina,
    int PersonalityLevel, int PersonalityUptie, int EgoUptie, int ItemCount,
    string Profile = "everything", string ItemProfile = "all");

/// <summary>
/// Local mod-menu UI. Deliberately NOT under /api/ so it bypasses AuthMiddleware: it is for
/// this machine only, and <see cref="LocalOnly"/> guards every route - against other machines
/// when the server also listens for friends, and against web pages steering a local browser.
/// </summary>
[ApiController]
[Route("tweak")]
[LocalOnly]
public sealed class TweakController(
    Config config,
    MyosotisDbContext db,
    DatabaseService databaseService,
    StaticDataService staticData,
    NameService names,
    AccountService accounts,
    ILogger<TweakController> logger) : ControllerBase
{
    private const string TicketLeft = "LEFT", TicketRight = "RIGHT", TicketEgoBg = "EGOBG";

    [HttpGet("")]
    public ContentResult Page() => Content(TweakPage.Html, "text/html; charset=utf-8");

    /// <summary>
    /// The app's own icon: MIRROR_ICON if set, else the local icon theme's, else mirror.png beside
    /// Config.json (the Windows package).
    /// </summary>
    [HttpGet("icon")]
    public IActionResult Icon()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidates =
        [
            Environment.GetEnvironmentVariable("MIRROR_ICON") ?? "",
            Path.Combine(home, ".local/share/icons/hicolor/128x128/apps/patchlc-mirror.png"),
            config.ResolvePath("mirror.png"),
        ];
        var path = candidates.FirstOrDefault(p => p.Length > 0 && Path.IsPathFullyQualified(p) && System.IO.File.Exists(p));
        return path is null ? NotFound() : PhysicalFile(path, "image/png");
    }

    // ── the Black Forest ────────────────────────────────────────────────────────────────

    static readonly Dictionary<string, (string File, string Type)> ForestAssets = new()
    {
        ["gate"] = ("blackforest-gate.png", "image/png"),
        ["backdrop"] = ("blackforest-jumpscare.png", "image/png"),
        ["cry"] = ("blackforest-cry.ogg", "audio/ogg"),
        ["clip"] = ("blackforest-jumpscare.webm", "video/webm"),
    };

    /// <summary>The Windows app's jumpscare: Edge opens this fullscreen when Greencliff dies.</summary>
    [HttpGet("blackforest/scare")]
    public ContentResult ForestScare() => Content("""
        <!doctype html><meta charset="utf-8"><title>The Black Forest</title>
        <style>html,body{margin:0;height:100%;background:#000;overflow:hidden;cursor:none}
        video{width:100%;height:100%;object-fit:cover}</style>
        <video src="/tweak/blackforest/clip" autoplay playsinline></video>
        <script>const v=document.querySelector("video");v.onended=v.onerror=()=>window.close();</script>
        """, "text/html; charset=utf-8");

    /// <summary>The gate's art, the bird's cry and the jumpscare, from this machine only: BLACKFOREST_DIR,
    /// the art folder beside the server (Linux), or blackforest/ beside Config.json (Windows).
    /// None of it ships with the server.</summary>
    [HttpGet("blackforest/{name}")]
    public IActionResult ForestAsset(string name)
    {
        if (!ForestAssets.TryGetValue(name, out var asset))
            return NotFound();
        var path = ForestFile(asset.File);
        return path is null ? NotFound() : PhysicalFile(path, asset.Type);
    }

    string? ForestFile(string file)
    {
        string[] dirs =
        [
            Environment.GetEnvironmentVariable("BLACKFOREST_DIR") ?? "",
            config.ResolvePath("../art"),
            config.ResolvePath("blackforest"),
        ];
        return dirs.Where(d => d.Length > 0 && Path.IsPathFullyQualified(d))
            .Select(d => Path.Combine(d, file)).FirstOrDefault(System.IO.File.Exists);
    }

    [HttpGet("api/account/{uid:long}/blackforest")]
    public IActionResult ForestState(long uid) => Ok(new { open = BlackForest.For(config).IsOpen(uid) });

    /// <summary>The client patch saw Greencliff die: while he is on in limbus-skins, the
    /// jumpscare plays, and the account in this machine's current fight wins it and finds the
    /// gate open.</summary>
    [HttpPost("api/blackforest/fell")]
    public IActionResult ForestFell()
    {
        if (!BlackForestScare.Armed())
            return Conflict(new { error = "greencliff is off in limbus-skins" });
        var uid = BlackForest.For(config).Fell();
        if (uid == 0)
            return Conflict(new { error = "no fight from this machine is known" });
        var why = BlackForestScare.Play(config, ForestFile("blackforest-jumpscare.mp4"));
        if (why is not null)
            logger.LogWarning("The jumpscare couldn't play: {Why}", why);
        logger.LogInformation("Greencliff fell; the Black Forest opened for uid {Uid}", uid);
        return Ok(new { uid, scare = why ?? "playing" });
    }

    [HttpPost("api/account/{uid:long}/blackforest/seal")]
    public IActionResult ForestSeal(long uid)
    {
        BlackForest.For(config).Seal(uid);
        return Ok(new { open = false });
    }

    // ── how the pages look ──────────────────────────────────────────────────────────────

    [HttpGet("api/theme")]
    public IActionResult GetTheme() => Ok(Look.Load(config));

    [HttpPost("api/theme")]
    public IActionResult SetTheme([FromBody] Look.Theme t)
    {
        var keep = Look.Load(config);
        t = t with { Picture = keep.Picture };                      // only the upload changes the picture
        if (!Look.Valid(t))
            return BadRequest(new { error = "background must be black, white, colour or picture; colours #rrggbb" });
        Look.Save(config, t);
        return Ok(t);
    }

    /// <summary>The picture behind the pages: the request body, PNG, JPEG or WebP, up to 8 MB.</summary>
    [HttpPost("api/theme/picture")]
    [RequestSizeLimit(Look.MaxPicture + 1)]
    public async Task<IActionResult> SetPicture()
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        if (ms.Length > Look.MaxPicture)
            return BadRequest(new { error = "pictures can be up to 8 MB" });
        var bytes = ms.ToArray();
        if (Look.PictureType(bytes) is null)
            return BadRequest(new { error = "that isn't a PNG, JPEG or WebP picture" });
        await System.IO.File.WriteAllBytesAsync(config.ResolvePath("theme-picture"), bytes);
        var t = Look.Load(config) with { Bg = "picture", Picture = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        Look.Save(config, t);
        return Ok(t);
    }

    [HttpGet("theme/picture")]
    public IActionResult GetPicture()
    {
        var path = config.ResolvePath("theme-picture");
        if (!System.IO.File.Exists(path))
            return NotFound();
        var head = new byte[16];
        using (var f = System.IO.File.OpenRead(path))
            f.ReadExactly(head, 0, Math.Min(16, (int)f.Length));
        var type = Look.PictureType(head);
        return type is null ? NotFound() : PhysicalFile(path, type);
    }

    [HttpGet("api/accounts")]
    public async Task<ActionResult<List<UserRow>>> Accounts() =>
        await db.Users.AsNoTracking()
            .OrderBy(u => u.Uid)
            .Select(u => new UserRow(u.Uid, u.Credential, u.Level, u.Exp, u.Stamina))
            .ToListAsync();

    [HttpGet("api/seed")]
    public ActionResult<SeedDto> GetSeed()
    {
        var s = config.Seed;
        return new SeedDto(s.GrantEverything, s.UserLevel, s.Stamina,
            s.PersonalityLevel, s.PersonalityUptie, s.EgoUptie, s.ItemCount, s.Profile, s.ItemProfile);
    }

    /// <summary>Persists new-account defaults back into Config.json and applies them live.</summary>
    [HttpPost("api/seed")]
    public ActionResult<object> SetSeed([FromBody] SeedDto dto)
    {
        var s = config.Seed;
        s.GrantEverything = dto.GrantEverything;
        s.UserLevel = Math.Clamp(dto.UserLevel, 1, 999);
        s.Stamina = Math.Clamp(dto.Stamina, 0, 999);
        s.PersonalityLevel = Math.Clamp(dto.PersonalityLevel, 1, 60);
        s.PersonalityUptie = Math.Clamp(dto.PersonalityUptie, 1, 4);
        s.EgoUptie = Math.Clamp(dto.EgoUptie, 1, 4);
        s.ItemCount = Math.Clamp(dto.ItemCount, 0, 999_999);
        s.Profile = dto.Profile is "everything" or "balanced" or "starter" or "empty"
            ? dto.Profile
            : "everything";
        s.ItemProfile = dto.ItemProfile is "all" or "curated" or "none" ? dto.ItemProfile : "all";

        var path = Path.Combine(config.Root, "Config.json");
        try
        {
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var root = doc.RootElement.Clone();
            using var stream = System.IO.File.Create(path);
            using var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            w.WriteStartObject();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.NameEquals("seed")) continue;
                prop.WriteTo(w);
            }
            w.WriteStartObject("seed");
            w.WriteBoolean("grantEverything", s.GrantEverything);
            w.WriteNumber("userLevel", s.UserLevel);
            w.WriteNumber("stamina", s.Stamina);
            w.WriteNumber("personalityLevel", s.PersonalityLevel);
            w.WriteNumber("personalityUptie", s.PersonalityUptie);
            w.WriteNumber("egoUptie", s.EgoUptie);
            w.WriteNumber("itemCount", s.ItemCount);
            w.WriteString("profile", s.Profile);
            w.WriteString("itemProfile", s.ItemProfile);
            w.WriteNumber("balancedRank2Every", s.BalancedRank2Every);
            w.WriteNumber("balancedRank3Every", s.BalancedRank3Every);
            w.WriteNumber("balancedEgoEvery", s.BalancedEgoEvery);
            w.WriteEndObject();
            w.WriteEndObject();
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not persist seed settings to {Path}: {Message}", path, ex.Message);
            return new { ok = true, saved = false, note = "applied for this session only: " + ex.Message };
        }

        logger.LogInformation("Seed defaults updated (grantEverything={Grant})", s.GrantEverything);
        return new { ok = true, saved = true };
    }

    /// <summary>Copy another account's entire collection onto this one. Overwrites.</summary>
    [HttpPost("api/account/{uid:long}/copy-from/{srcUid:long}")]
    public async Task<ActionResult<object>> CopyFrom(long uid, long srcUid)
    {
        if (uid == srcUid) return BadRequest(new { error = "same account" });
        var dst = await db.Users.FirstOrDefaultAsync(u => u.Uid == uid);
        var src = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uid == srcUid);
        if (dst is null || src is null) return NotFound();

        dst.Level = src.Level; dst.Exp = src.Exp; dst.Stamina = src.Stamina;

        var now = TimeUtil.UnixNow();

        db.UserPersonalities.RemoveRange(db.UserPersonalities.Where(x => x.Uid == uid));
        db.UserEgos.RemoveRange(db.UserEgos.Where(x => x.Uid == uid));
        db.UserItems.RemoveRange(db.UserItems.Where(x => x.Uid == uid));
        db.UserAnnouncers.RemoveRange(db.UserAnnouncers.Where(x => x.Uid == uid));
        db.UserBanners.RemoveRange(db.UserBanners.Where(x => x.Uid == uid));
        db.ProfileTickets.RemoveRange(db.ProfileTickets.Where(x => x.Uid == uid));
        await db.SaveChangesAsync();

        foreach (var r in await db.UserPersonalities.AsNoTracking().Where(x => x.Uid == srcUid).ToListAsync())
            db.UserPersonalities.Add(new UserPersonality { Uid = uid, PersonalityId = r.PersonalityId,
                Level = r.Level, Exp = r.Exp, Gacksung = r.Gacksung, OrderId = r.OrderId,
                GacksungIllustType = r.GacksungIllustType, AcquireTime = now });
        foreach (var r in await db.UserEgos.AsNoTracking().Where(x => x.Uid == srcUid).ToListAsync())
            db.UserEgos.Add(new UserEgo { Uid = uid, EgoId = r.EgoId, Gacksung = r.Gacksung, AcquireTime = now });
        foreach (var r in await db.UserItems.AsNoTracking().Where(x => x.Uid == srcUid).ToListAsync())
            db.UserItems.Add(new UserItem { Uid = uid, ItemId = r.ItemId, Num = r.Num });
        foreach (var r in await db.UserAnnouncers.AsNoTracking().Where(x => x.Uid == srcUid).ToListAsync())
            db.UserAnnouncers.Add(new UserAnnouncer { Uid = uid, AnnouncerId = r.AnnouncerId });
        foreach (var r in await db.UserBanners.AsNoTracking().Where(x => x.Uid == srcUid).ToListAsync())
            db.UserBanners.Add(new UserBanner { Uid = uid, Id = r.Id, Acquiretime = now,
                Value = r.Value, Value2 = r.Value2, Value3 = r.Value3, Value4 = r.Value4 });
        foreach (var r in await db.ProfileTickets.AsNoTracking().Where(x => x.Uid == srcUid).ToListAsync())
            db.ProfileTickets.Add(new ProfileTicket { Uid = uid, TicketType = r.TicketType, Id = r.Id, Date = now });

        await db.SaveChangesAsync();
        databaseService.Invalidate(uid);
        logger.LogInformation("Copied save from uid {Src} to uid {Dst}", srcUid, uid);
        return new { ok = true };
    }

    /// <summary>
    /// Wipe an account's contents and re-seed it from the current profile. Destructive: the
    /// account keeps its uid and token, everything it owned is replaced.
    /// </summary>
    [HttpPost("api/account/{uid:long}/reset")]
    public async Task<ActionResult<object>> ResetAccount(long uid)
    {
        if (!await db.Users.AnyAsync(u => u.Uid == uid))
            return NotFound(new { error = $"no account with uid {uid}" });

        await accounts.ResetAsync(uid);
        logger.LogInformation("Tweaker reset uid {Uid} to profile '{Profile}'", uid, config.Seed.Profile);
        return new { reset = uid, profile = config.Seed.Profile };
    }

    [HttpGet("api/account/{uid:long}")]
    public async Task<ActionResult<AccountState>> Account(long uid)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uid == uid);
        if (user is null)
            return NotFound();

        var ownedP = await db.UserPersonalities.AsNoTracking()
            .Where(x => x.Uid == uid).ToDictionaryAsync(x => x.PersonalityId, x => x);
        var ownedE = await db.UserEgos.AsNoTracking()
            .Where(x => x.Uid == uid).ToDictionaryAsync(x => x.EgoId, x => x);
        var itemRows = await db.UserItems.AsNoTracking()
            .Where(x => x.Uid == uid).ToDictionaryAsync(x => x.ItemId, x => x.Num);

        var personalities = staticData.PersonalityIds
            .Select(id =>
            {
                var n = names.ForPersonality(id);
                var has = ownedP.TryGetValue(id, out var row);
                return new PersonalityRow(id, n.Sinner, n.Title, n.Rank, has,
                    has ? row!.Level : 1, has ? row!.Gacksung : 1, has ? row!.GacksungIllustType : 1);
            })
            .OrderBy(p => p.Sinner).ThenBy(p => p.Id).ToList();

        var egos = staticData.EgoIds
            .Select(id =>
            {
                var n = names.ForEgo(id);
                var has = ownedE.TryGetValue(id, out var row);
                return new EgoRow(id, n.Sinner, has, has ? row!.Gacksung : 1);
            })
            .OrderBy(e => e.Name).ThenBy(e => e.Id).ToList();

        var items = staticData.ItemIds
            .Select(id => new ItemRow(id, itemRows.GetValueOrDefault(id), names.ForItem(id)))
            .OrderBy(i => i.Id).ToList();

        var announcers = await db.UserAnnouncers.AsNoTracking()
            .Where(x => x.Uid == uid).Select(x => x.AnnouncerId).ToListAsync();
        var banners = await db.UserBanners.AsNoTracking()
            .Where(x => x.Uid == uid).Select(x => x.Id).ToListAsync();
        var tickets = await db.ProfileTickets.AsNoTracking()
            .Where(x => x.Uid == uid).Select(x => new { x.TicketType, x.Id }).ToListAsync();

        List<int> Tickets(string t) => tickets.Where(x => x.TicketType == t).Select(x => x.Id).ToList();

        var collections = new List<CollectionState>
        {
            new("announcers", "Announcers",      [.. staticData.AnnouncerIds],  announcers),
            new("banners",    "Profile banners", [.. staticData.UserBannerIds], banners),
            new("borderL",    "Left borders",    [.. staticData.BorderLeftIds], Tickets(TicketLeft)),
            new("borderR",    "Right borders",   [.. staticData.BorderRightIds],Tickets(TicketRight)),
            new("egoBg",      "E.G.O. backgrounds", [.. staticData.EgoBgIds],   Tickets(TicketEgoBg)),
        };

        return new AccountState(
            new UserRow(user.Uid, user.Credential, user.Level, user.Exp, user.Stamina),
            personalities, egos, items, collections);
    }

    [HttpPost("api/account/{uid:long}")]
    public async Task<ActionResult<object>> Apply(long uid, [FromBody] ApplyRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Uid == uid);
        if (user is null)
            return NotFound();

        var now = TimeUtil.UnixNow();
        var changed = 0;

        if (req.User is not null)
        {
            if (req.User.Level is int lv) { user.Level = Math.Clamp(lv, 1, 999); changed++; }
            if (req.User.Exp is int xp) { user.Exp = Math.Max(0, xp); changed++; }
            if (req.User.Stamina is int st) { user.Stamina = Math.Clamp(st, 0, 999); changed++; }
        }

        changed += await ApplyPersonalitiesAsync(uid, req.Personalities, now);
        changed += await ApplyEgosAsync(uid, req.Egos, now);
        changed += await ApplyItemsAsync(uid, req.Items, req.ItemsSetAll);
        changed += await ApplyCollectionsAsync(uid, req.Collections, now);

        await db.SaveChangesAsync();
        databaseService.Invalidate(uid);
        logger.LogInformation("Tweaker applied {Changed} edits to uid {Uid}", changed, uid);
        return new { ok = true, changed };
    }

    private async Task<int> ApplyPersonalitiesAsync(long uid, List<PersonalityEdit>? edits, long now)
    {
        if (edits is not { Count: > 0 }) return 0;
        var valid = staticData.PersonalityIds.ToHashSet();
        var list = edits.Where(e => valid.Contains(e.Id)).ToList();
        var ids = list.Select(e => e.Id).ToHashSet();
        var existing = await db.UserPersonalities
            .Where(x => x.Uid == uid && ids.Contains(x.PersonalityId))
            .ToDictionaryAsync(x => x.PersonalityId, x => x);

        var n = 0;
        foreach (var e in list)
        {
            var level = Math.Clamp(e.Level, 1, 60);
            var uptie = Math.Clamp(e.Uptie, 1, 4);
            var illust = Math.Clamp(e.Illust ?? 1, 1, 9);
            existing.TryGetValue(e.Id, out var row);

            if (!e.Owned)
            {
                if (row is not null) { db.UserPersonalities.Remove(row); n++; }
                continue;
            }
            if (row is null)
            {
                db.UserPersonalities.Add(new UserPersonality
                {
                    Uid = uid, PersonalityId = e.Id, Level = level, Exp = 0, Gacksung = uptie,
                    OrderId = ProfileUtil.GetOrderId(e.Id), GacksungIllustType = illust, AcquireTime = now,
                });
            }
            else { row.Level = level; row.Gacksung = uptie; row.GacksungIllustType = illust; }
            n++;
        }
        return n;
    }

    private async Task<int> ApplyEgosAsync(long uid, List<EgoEdit>? edits, long now)
    {
        if (edits is not { Count: > 0 }) return 0;
        var valid = staticData.EgoIds.ToHashSet();
        var list = edits.Where(e => valid.Contains(e.Id)).ToList();
        var ids = list.Select(e => e.Id).ToHashSet();
        var existing = await db.UserEgos
            .Where(x => x.Uid == uid && ids.Contains(x.EgoId))
            .ToDictionaryAsync(x => x.EgoId, x => x);

        var n = 0;
        foreach (var e in list)
        {
            var uptie = Math.Clamp(e.Uptie, 1, 4);
            existing.TryGetValue(e.Id, out var row);
            if (!e.Owned)
            {
                if (row is not null) { db.UserEgos.Remove(row); n++; }
                continue;
            }
            if (row is null)
                db.UserEgos.Add(new UserEgo { Uid = uid, EgoId = e.Id, Gacksung = uptie, AcquireTime = now });
            else
                row.Gacksung = uptie;
            n++;
        }
        return n;
    }

    private async Task<int> ApplyItemsAsync(long uid, List<ItemEdit>? edits, int? setAll)
    {
        var rows = await db.UserItems.Where(x => x.Uid == uid).ToListAsync();
        var byId = rows.ToDictionary(r => r.ItemId, r => r);
        var n = 0;

        if (setAll is int all)
        {
            all = Math.Clamp(all, 0, 999_999);
            foreach (var r in rows) r.Num = all;
            foreach (var id in staticData.ItemIds.Where(id => !byId.ContainsKey(id)))
                db.UserItems.Add(new UserItem { Uid = uid, ItemId = id, Num = all });
            n++;
        }

        if (edits is { Count: > 0 })
        {
            var valid = staticData.ItemIds.ToHashSet();
            foreach (var e in edits.Where(e => valid.Contains(e.Id)))
            {
                var num = Math.Clamp(e.Num, 0, 999_999);
                if (byId.TryGetValue(e.Id, out var row)) row.Num = num;
                else db.UserItems.Add(new UserItem { Uid = uid, ItemId = e.Id, Num = num });
                n++;
            }
        }
        return n;
    }

    private async Task<int> ApplyCollectionsAsync(long uid, Dictionary<string, List<int>>? cols, long now)
    {
        if (cols is not { Count: > 0 }) return 0;
        var n = 0;

        if (cols.TryGetValue("announcers", out var wantAnn))
        {
            var valid = staticData.AnnouncerIds.ToHashSet();
            var want = wantAnn.Where(valid.Contains).ToHashSet();
            var have = await db.UserAnnouncers.Where(x => x.Uid == uid).ToListAsync();
            foreach (var r in have.Where(r => !want.Contains(r.AnnouncerId))) { db.UserAnnouncers.Remove(r); n++; }
            foreach (var id in want.Except(have.Select(r => r.AnnouncerId)))
            { db.UserAnnouncers.Add(new UserAnnouncer { Uid = uid, AnnouncerId = id }); n++; }
        }

        if (cols.TryGetValue("banners", out var wantBan))
        {
            var valid = staticData.UserBannerIds.ToHashSet();
            var want = wantBan.Where(valid.Contains).ToHashSet();
            var have = await db.UserBanners.Where(x => x.Uid == uid).ToListAsync();
            foreach (var r in have.Where(r => !want.Contains(r.Id))) { db.UserBanners.Remove(r); n++; }
            foreach (var id in want.Except(have.Select(r => r.Id)))
            {
                var (v1, v2) = SeedValues.BannerValue(id);
                db.UserBanners.Add(new UserBanner { Uid = uid, Id = id, Acquiretime = now, Value = v1, Value2 = v2 });
                n++;
            }
        }

        foreach (var (key, type, valid) in new[]
                 {
                     ("borderL", TicketLeft,  staticData.BorderLeftIds),
                     ("borderR", TicketRight, staticData.BorderRightIds),
                     ("egoBg",   TicketEgoBg, staticData.EgoBgIds),
                 })
        {
            if (!cols.TryGetValue(key, out var wantIds)) continue;
            var ok = valid.ToHashSet();
            var want = wantIds.Where(ok.Contains).ToHashSet();
            var have = await db.ProfileTickets.Where(x => x.Uid == uid && x.TicketType == type).ToListAsync();
            foreach (var r in have.Where(r => !want.Contains(r.Id))) { db.ProfileTickets.Remove(r); n++; }
            foreach (var id in want.Except(have.Select(r => r.Id)))
            { db.ProfileTickets.Add(new ProfileTicket { Uid = uid, TicketType = type, Id = id, Date = now }); n++; }
        }

        return n;
    }
}

/// <summary>
/// Requests have to come from this machine - the menu lists every account's credential and
/// can reset any of them, so a friend reaching the server over Tailscale must not get it. They
/// also have to name this server by its loopback address, so a page that points its own domain
/// at 127.0.0.1 (DNS rebinding) is refused, and every write has to carry an X-Mirror header,
/// which a page from anywhere else can't add without the server's consent (it never gives it) -
/// so a stray link can't reset an account.
/// </summary>
public sealed class LocalOnlyAttribute : Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute
{
    /// <summary>Whether the request's peer is this machine.</summary>
    public static bool IsLoopback(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is { IsIPv4MappedToIPv6: true })
            remote = remote.MapToIPv4();
        return remote is not null && System.Net.IPAddress.IsLoopback(remote);
    }

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        if (!IsLoopback(context.HttpContext))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        var request = context.HttpContext.Request;
        var host = request.Host.Host;
        if (host is not ("127.0.0.1" or "localhost" or "[::1]"))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)
            && request.Headers["X-Mirror"] != "1")
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
    }
}
