using Microsoft.AspNetCore.Mvc;

namespace Ray.BiliBiliTool.Web.Controllers;

[ApiController]
[Route("test")]
public class TestController(IConfiguration config) : ControllerBase
{
    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        await Task.Delay(1);
        var testConfig = config["DailyTaskConfig:NumberOfCoins"];
        return Ok(testConfig);
    }
}
