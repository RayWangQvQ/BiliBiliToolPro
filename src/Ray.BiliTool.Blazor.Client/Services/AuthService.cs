using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Ray.BiliTool.Blazor.Client.Models;
using Ray.BiliTool.Blazor.Models;
using static System.Net.WebRequestMethods;

namespace Ray.BiliTool.Blazor.Client.Services
{
    public interface IAuthService
    {
        Task<UserDto> Login(LoginParamsType rqtDto);

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

        public async Task<UserDto> Login(LoginParamsType rqtDto)
        {
            var httpResponse = await _httpClient.PostAsJsonAsync<LoginParamsType>($"api/Auth/Login", rqtDto);
            UserDto result = await httpResponse.Content.ReadFromJsonAsync<UserDto>();

            if (result != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.Token);
            }

            return result;
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
