using System.ComponentModel;

namespace Ray.BiliTool.Blazor.Web.Pages.Configs.UnfollowBatched
{
    public class UnfollowBatchedTaskConfigModel
    {
        [DisplayName("分组名称")]
        public string GroupName { get; set; }

        [DisplayName("取关数量")]
        public int Count { get; set; } = 0;

        [DisplayName("UP白名单")]
        public string RetainUids { get; set; }
    }
}
