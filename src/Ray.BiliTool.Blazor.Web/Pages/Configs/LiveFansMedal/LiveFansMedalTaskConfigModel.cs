namespace Ray.BiliTool.Blazor.Web.Pages.Configs.LiveFansMedal
{
    public class LiveFansMedalTaskConfigModel
    {
        /// <summary>
        /// 自定义发送弹幕内容，如 “打卡” 等来触发直播间内机器人关键词
        /// </summary>
        public string DanmakuContent { get; set; } = "OvO";

        /// <summary>
        /// 心跳包发送的个数 / 挂机的时间，单位为分钟
        /// </summary>
        public int HeartBeatNumber { get; set; } = 70;

        /// <summary>
        /// 当心跳包发送连续失败多少次时放弃
        /// </summary>
        public int HeartBeatSendGiveUpThreshold { get; set; } = 5;

        /// <summary>
        /// 对于直播时长任务是否跳过粉丝牌等级大于等于 20 的
        /// </summary>
        public bool IsSkipLevel20Medal { get; set; } = true;
    }
}
