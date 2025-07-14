using Microsoft.AspNetCore.Mvc;

namespace Ray.BiliBiliTool.Web.Controllers;

[Route("test")]
public class TestController(IConfiguration config) : Controller
{
    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        await Task.Delay(1);
        var testConfig = config["DailyTaskConfig:NumberOfCoins"];
        return Ok(testConfig);
    }
}
