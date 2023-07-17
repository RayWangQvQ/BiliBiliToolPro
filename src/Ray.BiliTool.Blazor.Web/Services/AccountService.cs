using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ray.BiliBiliTool.Infrastructure;
using Ray.BiliTool.Blazor.Web.Models;
using Ray.BiliTool.Blazor.Web.Pages.Account.Center;
using Ray.DDD;

namespace Ray.BiliTool.Blazor.Web.Services
{
    public interface IAccountService
    {
        Task<List<AccountManage>> GetListAsync();
        Task PutSync(string id, AccountManage model);
        Task<bool> DeleteAsync(string id);
    }

    public class AccountService : IAccountService
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<List<AccountManage>> GetListAsync()
        {
            List<IdentityUser> users = await _userManager.Users.ToListAsync();

            var result = new List<AccountManage>();

            foreach (var user in users)
            {
                var role = await GetRoleByUserAsync(user);
                result.Add(new AccountManage
                {
                    Id = user.Id,
                    Email = user.Email,
                    RoleType = role
                });
            }

            return result;
        }

        public async Task PutSync(string id, AccountManage model)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                throw new EntityNotFoundException("用户不存在");
            }

            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.UserName = model.Email;

                await _userManager.UpdateAsync(user);
            }

            var role = await GetRoleByUserAsync(user);
            if (role == model.RoleType) return;

            if (role != RoleType.Non)
            {
                var rmResult = await _userManager.RemoveFromRolesAsync(user, new[] { role.ToString() });
            }

            if (model.RoleType != RoleType.Non)
            {
                var addResult = await _userManager.AddToRoleAsync(user, model.RoleType.ToString());
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                throw new EntityNotFoundException("用户不存在");
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        private async Task<RoleType> GetRoleByUserAsync(IdentityUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleStr = roles.FirstOrDefault();
            bool result = Enum.TryParse(roleStr, out RoleType role);
            return result ? role : RoleType.Non;
        }
    }
}
