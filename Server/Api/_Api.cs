using AspApiController = Microsoft.AspNetCore.Mvc.ApiControllerAttribute;
using Microsoft.AspNetCore.Mvc;

namespace Server.Api;

[AspApiController]
[Route("api")]
public partial class ApiController : ControllerBase
{
}
