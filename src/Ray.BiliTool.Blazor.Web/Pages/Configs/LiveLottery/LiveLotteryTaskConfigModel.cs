using System.ComponentModel;

namespace Ray.BiliTool.Blazor.Web.Pages.Configs.LiveLottery
{
    public class LiveLotteryTaskConfigModel
    {
        [DisplayName("排除关键字")]
        public string ExcludeAwardNames { get; set; }

        [DisplayName("包含关键字")]
        public string IncludeAwardNames { get; set; }

        [DisplayName("开启自动分组")]
        public bool AutoGroupFollowings { get; set; } = true;

        [DisplayName("屏蔽UPs")]
        public string DenyUids { get; set; }
    }
}
