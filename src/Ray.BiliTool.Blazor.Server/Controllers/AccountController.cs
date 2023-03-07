using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliTool.Blazor.Server.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [Route("/Account/Login")]
        public async Task SignInAsync([FromQuery]string uname, [FromQuery] string pwd)
        {
            var adminName = _config["Admin:UserName"];
            var adminPwd = _config["Admin:Pwd"];

            if (uname != adminName || pwd != adminPwd)
            {
                await HttpContext.ChallengeAsync();
                return;
            }

            var identity = new GenericIdentity(uname, pwd);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(principal);
        }
    }
}
