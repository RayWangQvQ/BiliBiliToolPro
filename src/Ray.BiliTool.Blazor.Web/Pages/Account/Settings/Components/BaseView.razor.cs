using System.Threading.Tasks;
using Ray.BiliTool.Blazor.Web.Models;
using Ray.BiliTool.Blazor.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Ray.BiliTool.Blazor.Web.Pages.Account.Settings
{
    public partial class BaseView
    {
        private CurrentUser _currentUser = new CurrentUser();

        [Inject] protected IUserService UserService { get; set; }

        private void HandleFinish()
        {
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            _currentUser = await UserService.GetCurrentUserAsync();
        }
    }
}