using System.Threading.Tasks;
using Ray.BiliTool.Blazor.Models;
using Ray.BiliTool.Blazor.Services;
using Microsoft.AspNetCore.Components;
using AntDesign;
using Ray.BiliTool.Blazor.Client.Services;
using Ray.BiliTool.Blazor.Client;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace Ray.BiliTool.Blazor.Pages.User
{
    public partial class Login
    {
        private readonly LoginParamsType _model = new LoginParamsType();

        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] public IAccountService AccountService { get; set; }
        [Inject] public IAuthService AuthService { get; set; }

        [Inject] public MessageService Message { get; set; }

        [Inject] public MessageService MsgSvr { get; set; }
        [Inject] public AuthenticationStateProvider AuthProvider { get; set; }

        public async Task HandleSubmit()
        {
            var result = await AuthService.Login(new LoginParamsType()
            {
                UserName = _model.UserName,
                Password = _model.Password,
            });

            if (string.IsNullOrWhiteSpace(result?.Token) == false)
            {
                MsgSvr.Success($"登录成功");
                
                ((AuthProvider)AuthProvider).MarkUserAsAuthenticated(result);
            }
            else
            {
                MsgSvr.Error($"用户名或密码错误");
            }
            //isLoading = false;
            InvokeAsync(StateHasChanged);

            NavigationManager.NavigateTo("/");
        }

        public async Task GetCaptcha()
        {
            var captcha = await AccountService.GetCaptchaAsync(_model.Mobile);
            await Message.Success($"Verification code validated successfully! The verification code is: {captcha}");
        }
    }
}
