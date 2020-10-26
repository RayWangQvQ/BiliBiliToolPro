using System;

namespace Ray.BiliBiliTool.Config
{
    public class BiliBiliCookiesOptions
    {
        public BiliBiliCookiesOptions()
        {

        }

        public BiliBiliCookiesOptions(String userId, String sessData, String biliJct)
        {
            UserId = userId;
            SessData = sessData;
            BiliJct = biliJct;
        }

        public string UserId { get; set; }

        public string SessData { get; set; }

        public string BiliJct { get; set; }

        public override string ToString()
        {
            return $"bili_jct={BiliJct};SESSDATA={SessData};DedeUserID={UserId}";
        }
    }
}
