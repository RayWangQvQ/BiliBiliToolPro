using Ray.BiliBiliTool.Agent;
using Ray.BiliTool.Domain;

namespace Ray.BiliTool.Blazor.Web.Pages.BiliAccount
{
    public class BiliCookieModel:DbConfig
    {
        public BiliCookieModel(DbConfig config) : base(config.ConfigKey, config.ConfigValue)
        {
            Id=config.Id;
            Enable=config.Enable;
            CreateTime=config.CreateTime;
            UpdateTime=config.UpdateTime;
        }

        public BiliCookie BiliCookie => new BiliCookie(this.ConfigValue);
    }
}
