using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Ray.BiliTool.Blazor.Models;

namespace Ray.BiliTool.Blazor.Client.Services
{
    public interface IAuthService
    {
        Task<bool> Login(LoginParamsType rqtDto);

        Task Logout();
    }

    internal class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ILocalStorageService _localStorage;

        public AuthService(HttpClient httpClient,
            AuthenticationStateProvider authenticationStateProvider,
            ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = authenticationStateProvider;
            _localStorage = localStorage;
        }

        public async Task<bool> Login(LoginParamsType rqtDto)
        {
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new(nameof(LoginParamsType.UserName), rqtDto.UserName),
                new(nameof(LoginParamsType.Password), rqtDto.Password),
            });
            using var rsp = await _httpClient.PostAsync("/account/login", content);
            if (!rsp.IsSuccessStatusCode)
            {
                return false;
            }
            var authToken = await rsp.Content.ReadAsStringAsync();
            await _localStorage.SetItemAsync("authToken", authToken);
            ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(rqtDto.UserName);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            return true;
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((ApiAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public class LoginRqtDto
    {
        public string Account { get; set; }

        public string Password { get; set; }
    }
}
