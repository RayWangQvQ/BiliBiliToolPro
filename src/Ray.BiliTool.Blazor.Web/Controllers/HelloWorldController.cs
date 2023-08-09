using Microsoft.AspNetCore.Mvc;

namespace Ray.BiliTool.Blazor.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HelloWorldController : Controller
    {
        [HttpGet]
        public string Get()
        {
            return "Hello World";
        }
    }
}
