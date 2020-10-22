using System;
using System.Collections.Generic;
using System.Text;

namespace BiliBiliTool.Login
{
    public class Verify
    {

        public String UserId { get; private set; }
        public String SessData { get; private set; }
        public String BiliJct { get; private set; }

        public Verify(String userId, String sessData, String biliJct)
        {
            UserId = userId;
            SessData = sessData;
            BiliJct = biliJct;
        }

        public String getUserId()
        {
            return UserId;
        }

        public String getSessData()
        {
            return SessData;
        }

        public String getBiliJct()
        {
            return BiliJct;
        }

        public String getVerify()
        {
            return "\"bili_jct=" + getBiliJct() + ";SESSDATA=" + getSessData() + ";DedeUserID=" + getUserId();
        }
    }
}
