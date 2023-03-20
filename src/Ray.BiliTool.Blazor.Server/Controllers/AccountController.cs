using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ray.BiliTool.Blazor.Server.Options;
using Ray.BiliTool.Blazor.Models;

namespace Ray.BiliTool.Blazor.Server.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IOptionsMonitor<JwtOption> _jwtOpt;

        public AccountController(IConfiguration config, IOptionsMonitor<JwtOption> jwtOpt)
        {
            _config = config;
            _jwtOpt = jwtOpt;
        }

        [HttpPost]
        [Route("/Account/Login")]
        public async Task<IActionResult> SignInAsync([FromForm] LoginParamsType rqt)
        {
            var adminName = _config["Admin:UserName"];
            var adminPwd = _config["Admin:Pwd"];

            if (rqt.UserName != adminName || rqt.Password != adminPwd)
            {
                return Unauthorized();
            }

            var claims = new Claim[]
            {
                new(ClaimTypes.NameIdentifier, "0"),
                new(ClaimTypes.Name, adminName),
                new(ClaimTypes.Role, "admin")
            };
            var jwtSetting = _jwtOpt.CurrentValue;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSetting.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddHours(jwtSetting.ExpiryInHours);
            var token = new JwtSecurityToken(jwtSetting.Issuer, jwtSetting.Audience, claims, expires: expiry, signingCredentials: creds);
            var tokenText = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(tokenText);
        }
    }
}
