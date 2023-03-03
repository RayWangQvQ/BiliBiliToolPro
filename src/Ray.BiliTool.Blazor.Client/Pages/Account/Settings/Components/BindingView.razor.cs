using Ray.BiliTool.Blazor.Models;

namespace Ray.BiliTool.Blazor.Pages.Account.Settings
{
    public partial class BindingView
    {
        private readonly UserLiteItem[] _data =
        {
            new UserLiteItem
            {
                Avater = "taobao",
                Title = "Binding Taobao",
                Description = "Currently unbound Taobao account"
            },
            new UserLiteItem
            {
                Avater = "alipay",
                Title = "Binding Alipay",
                Description = "Currently unbound Alipay account"
            },
            new UserLiteItem
            {
                Avater = "dingding",
                Title = "Binding DingTalk",
                Description = "Currently unbound DingTalk account"
            }
        };
    }
}