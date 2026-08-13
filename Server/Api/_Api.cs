using AspApiController = Microsoft.AspNetCore.Mvc.ApiControllerAttribute;
using Common;
using Database;
using Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Resource;

namespace Server.Api;

[AspApiController]
[Route("api")]
public partial class ApiController : ControllerBase
{
    protected readonly DatabaseService _db;
    protected readonly StaticDataService _staticData;
    protected readonly AccountService _accounts;
    protected readonly UserRepository _users;
    protected readonly PersonalityRepository _personalities;
    protected readonly EgoRepository _egos;
    protected readonly ItemRepository _items;
    protected readonly FormationRepository _formations;
    protected readonly AnnouncerRepository _announcers;
    protected readonly BannerRepository _banners;
    protected readonly TicketRepository _tickets;
    protected readonly ProfileRepository _profiles;
    protected readonly RailwayRepository _railway;
    protected readonly Config _config;
    protected readonly ILogger<ApiController> _logger;

    public ApiController(
        DatabaseService db,
        StaticDataService staticData,
        AccountService accounts,
        UserRepository users,
        PersonalityRepository personalities,
        EgoRepository egos,
        ItemRepository items,
        FormationRepository formations,
        AnnouncerRepository announcers,
        BannerRepository banners,
        TicketRepository tickets,
        ProfileRepository profiles,
        RailwayRepository railway,
        Config config,
        ILogger<ApiController> logger)
    {
        _db = db;
        _staticData = staticData;
        _accounts = accounts;
        _users = users;
        _personalities = personalities;
        _egos = egos;
        _items = items;
        _formations = formations;
        _announcers = announcers;
        _banners = banners;
        _tickets = tickets;
        _profiles = profiles;
        _railway = railway;
        _config = config;
        _logger = logger;
    }
}
