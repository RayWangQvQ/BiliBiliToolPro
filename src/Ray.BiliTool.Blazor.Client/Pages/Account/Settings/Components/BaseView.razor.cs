using System.Threading.Tasks;
using Ray.BiliTool.Blazor.Models;
using Ray.BiliTool.Blazor.Services;
using Microsoft.AspNetCore.Components;

namespace Ray.BiliTool.Blazor.Pages.Account.Settings
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