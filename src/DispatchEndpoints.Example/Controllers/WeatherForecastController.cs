using Microsoft.AspNetCore.Mvc;

namespace DispatchEndpoints.Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Ping()
        {
            return Ok("Pong");
        }
    }
}