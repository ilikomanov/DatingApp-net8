using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ServiceFilter(typeof(LogUserActivity))]
[ApiController]
[Route("api/[controller]")] //[controller] will be replaced with the name of the class - /api/users
public class BaseApiController : ControllerBase
{

}
