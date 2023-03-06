using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authentication;

namespace Ray.BiliTool.Blazor.Server.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        [Route("/Account/Login")]
        public async Task SignInAsync()
        {
            var identity = new GenericIdentity("Foo", "Passord");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(principal);
        }
    }
}
