using System.Threading.Tasks;
using Ray.BiliTool.Blazor.Models;
using Ray.BiliTool.Blazor.Services;
using Microsoft.AspNetCore.Components;
using AntDesign;
using Ray.BiliTool.Blazor.Client.Services;

namespace Ray.BiliTool.Blazor.Pages.User
{
    public partial class Login
    {
        private readonly LoginParamsType _model = new LoginParamsType();

        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] public IAccountService AccountService { get; set; }
        [Inject] public IAuthService AuthService { get; set; }

        [Inject] public MessageService Message { get; set; }

        public async Task HandleSubmit()
        {
            var result = await AuthService.Login(new LoginParamsType()
            {
                UserName = _model.UserName,
                Password = _model.Password,
            });

            if (!result)
            {
                return;
            }

            NavigationManager.NavigateTo("/");
        }

        public async Task GetCaptcha()
        {
            var captcha = await AccountService.GetCaptchaAsync(_model.Mobile);
            await Message.Success($"Verification code validated successfully! The verification code is: {captcha}");
        }
    }
}
