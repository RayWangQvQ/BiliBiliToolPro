using Microsoft.Extensions.Logging;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Interfaces;

namespace Ray.BiliTool.Blazor.Web.Hangfire
{
    public class TestService
    {
        private readonly ILogger<TestService> _logger;
        private readonly BiliCookie _biliCookie;
        private readonly IUserInfoApi _userInfoApi;

        public TestService(ILogger<TestService> logger, BiliCookie biliCookie, IUserInfoApi userInfoApi)
        {
            _logger = logger;
            _biliCookie = biliCookie;
            _userInfoApi = userInfoApi;
        }

        public void Run()
        {
            _logger.LogInformation("success");
        }
    }
}
