using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliTool.Repository
{
    public class IdentitySeed
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _rolesManager;
        private readonly ILogger<IdentitySeed> _logger;

        public IdentitySeed(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<IdentitySeed> logger)
        {
            _userManager = userManager;
            _rolesManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            var roleNames = new String[] { RoleType.Admin.ToString(), RoleType.Manager.ToString(), RoleType.Guest.ToString() };

            foreach (var roleName in roleNames)
            {
                var role = await _rolesManager.RoleExistsAsync(roleName);
                if (!role)
                {
                    var result = await _rolesManager.CreateAsync(new IdentityRole
                    {
                        Id = roleName,
                        Name = roleName
                    });
                    _logger.LogInformation("Create {0}: {1}", roleName, result.Succeeded);
                }
            }
            // administrator
            var user = new IdentityUser
            {
                Id = "0",
                UserName = "admin@bili.tool",
                Email = "admin@bili.tool",
                EmailConfirmed = true
            };
            var i = await _userManager.FindByEmailAsync(user.Email);
            if (i == null)
            {
                var adminUser = await _userManager.CreateAsync(user, "1234@qweR");
                if (adminUser.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, RoleType.Admin.ToString());
                    _logger.LogInformation("Create {0}", user.UserName);
                }
            }
        }
    }
}
