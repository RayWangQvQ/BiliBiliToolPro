using Microsoft.AspNetCore.Mvc;

namespace Ray.BiliTool.Blazor.Web.Controllers
{
    [Route("api/{controller}")]
    public class HelloWorldController : Controller
    {
        public string Get()
        {
            return "Hello World";
        }
    }
}
