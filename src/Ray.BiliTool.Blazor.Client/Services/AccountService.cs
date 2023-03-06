using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ray.BiliTool.Blazor.Models;

namespace Ray.BiliTool.Blazor.Services
{
    public interface IAccountService
    {
        Task LoginAsync(LoginParamsType model);
        Task<string> GetCaptchaAsync(string modile);
    }

    public class AccountService : IAccountService
    {
        private readonly HttpClient _httpClient;
        private readonly Random _random = new Random();

        public AccountService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task LoginAsync(LoginParamsType model)
        {
            await _httpClient.GetAsync("Account/Login");
            // todo: login logic
            //return Task.CompletedTask;
        }

        public Task<string> GetCaptchaAsync(string modile)
        {
            var captcha = _random.Next(0, 9999).ToString().PadLeft(4, '0');
            return Task.FromResult(captcha);
        }
    }
}
