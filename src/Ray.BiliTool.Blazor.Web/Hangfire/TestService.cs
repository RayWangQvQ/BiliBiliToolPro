using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

namespace Ray.BiliTool.Blazor.Web.Hangfire
{
    public class TestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly BiliCookieContainer _biliCookieContainer;
        private readonly IUserInfoApi _userInfoApi;

        public TestService(ILogger<TestService> logger, BiliCookieContainer biliCookieContainer, IUserInfoApi userInfoApi)
        {
            _logger = logger;
            _biliCookieContainer = biliCookieContainer;
            _userInfoApi = userInfoApi;
        }

        public void Run()
        {
            _logger.LogInformation("success");
        }
    }
}
