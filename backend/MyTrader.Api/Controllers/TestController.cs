using Microsoft.AspNetCore.Mvc;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/v1/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(new { message = "Test controller working", timestamp = DateTime.UtcNow });
    }
    
    [HttpPost("echo")]
    public ActionResult Echo([FromBody] object data)
    {
        return Ok(new { echo = data, timestamp = DateTime.UtcNow });
    }
}