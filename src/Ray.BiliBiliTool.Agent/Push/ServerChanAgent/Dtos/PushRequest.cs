using Refit;

namespace Ray.BiliBiliTool.Agent.Push.ServerChanAgent.Dtos
{
    public class PushRequest
    {
        /// <summary>
        /// 标题
        /// </summary>
        [AliasAs("text")]
        public string Text { get; set; }

        /// <summary>
        /// 内容
        /// </summary>
        [AliasAs("desp")]
        public string Desp { get; set; }
    }
}
