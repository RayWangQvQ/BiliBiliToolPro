using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ray.BiliTool.Blazor.Server.Options;
using Ray.BiliTool.Blazor.Models;
using Ray.BiliTool.Blazor.Client.Models;

namespace Ray.BiliTool.Blazor.Server.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IOptionsMonitor<JwtOption> _jwtOpt;

        public AuthController(IConfiguration config, IOptionsMonitor<JwtOption> jwtOpt)
        {
            _config = config;
            _jwtOpt = jwtOpt;
        }

        [HttpPost]
        [Route("api/[controller]/login")]
        public async Task<UserDto> SignInAsync([FromBody] LoginParamsType rqt)
        {
            var adminName = _config["Admin:UserName"];
            var adminPwd = _config["Admin:Pwd"];

            var jwtToken = "";
            if (rqt.UserName != adminName || rqt.Password != adminPwd)
            {
                jwtToken = "";
            }
            else
            {
                jwtToken = GetToken(adminName);
            }

            return new() { Name = rqt.UserName, Token = jwtToken };
        }

        //获得用户，当页面客户端页面刷新时调用以获得用户信息
        [HttpGet]
        [Route("api/[controller]/GetUser")]
        public UserDto GetUser()
        {
            if (User.Identity.IsAuthenticated)//如果Token有效
            {
                var name = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;//从Token中拿出用户ID
                //模拟获得Token
                var jwtToken = GetToken(name);

                return new UserDto() { Name = name, Token = jwtToken };
            }
            else
            {
                return new UserDto() { Name = null, Token = null };
            }

        }

        public string GetToken(string name)
        {
            //此处加入账号密码验证代码

            var claims = new Claim[]
            {
                new Claim(ClaimTypes.Name,name),
                new Claim(ClaimTypes.Role,"Admin"),
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("123456789012345678901234567890123456789"));
            var expires = DateTime.Now.AddDays(30);
            var token = new JwtSecurityToken(
                issuer: "guetServer",
                audience: "guetClient",
                claims: claims,
                notBefore: DateTime.Now,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
