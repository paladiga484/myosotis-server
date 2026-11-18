using Microsoft.AspNetCore.Mvc;

namespace Server.Login;

[ApiController]
[Route("login")]
public partial class LoginController : ControllerBase
{
    protected readonly AccountService _accounts;

    public LoginController(AccountService accounts)
    {
        _accounts = accounts;
    }
}
