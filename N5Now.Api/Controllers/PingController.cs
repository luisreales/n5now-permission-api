using Microsoft.AspNetCore.Mvc;

namespace N5Now.Api.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("pong");
    }
}
