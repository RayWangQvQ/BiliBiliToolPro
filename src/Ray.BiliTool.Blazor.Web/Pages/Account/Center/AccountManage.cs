using System.ComponentModel.DataAnnotations;
using Ray.BiliBiliTool.Infrastructure;

namespace Ray.BiliTool.Blazor.Web.Pages.Account.Center
{
    public class AccountManage
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsManager { get; set; }

        public RoleType RoleType { get; set; }
    }
}
