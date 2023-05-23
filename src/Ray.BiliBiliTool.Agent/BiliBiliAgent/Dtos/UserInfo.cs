using System.Linq;
using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    /// <summary>
    /// 账户信息
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// 用户Id
        /// </summary>
        public long Mid { get; set; }//todo:这里登陆后可以获取到自己的UserId，后面可以考虑将配置Cookie项去除UserId的配置，改为登陆后获取

        /// <summary>
        /// 是否登录
        /// </summary>
        public bool IsLogin { get; set; }

        /// <summary>
        /// 等级信息
        /// </summary>
        public LevelInfo Level_info { get; set; }

        public decimal? Money { get; set; }

        public string Uname { get; set; }

        public Wallet Wallet { get; set; }

        /// <summary>
        /// 会员状态
        /// <para>只有VipStatus为1的时候获取到VipType才是有效的</para>
        /// </summary>
        public VipStatus VipStatus { get; set; }

        public VipType VipType { get; set; }

        /// <summary>
        /// 获取隐私处理后的用户名
        /// </summary>
        /// <returns></returns>
        public string GetFuzzyUname()
        {
            StringBuilder sb = new StringBuilder();
            int s1 = Uname.Length / 2;
            int s2 = (s1 + 1) / 2;
            for (int i = 0; i < Uname.Length; i++)
            {
                if (i >= s2 && i < s1 + s2) sb.Append("x");
                else sb.Append(Uname[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 返回会员类型
        /// </summary>
        /// <returns>
        /// <para> 0:无会员（会员过期、当前不是会员）</para>
        /// <para>1:月会员</para>
        /// <para>2:年会员</para>
        /// </returns>
        public VipType GetVipType()
        {
            if (VipStatus == VipStatus.Enable)
            {
                //只有VipStatus为1的时候获取到VipType才是有效的。
                return VipType;
            }
            else
            {
                return VipType.None;
            }
        }

        /// <summary>
        /// 防爬加密用的
        /// </summary>
        public WbiImg Wbi_img { get; set; }
    }

    /// <summary>
    /// 会员等级
    /// </summary>
    public class LevelInfo
    {
        /// <summary>
        /// 当前等级
        /// </summary>
        public int Current_level { get; set; }

        //public long Current_min { get; set; }

        /// <summary>
        /// 当前经验值
        /// </summary>
        public long Current_exp { get; set; }

        private long _next_exp;

        /// <summary>
        /// 下一升级经验值（因为Lv6的带佬会返回字符串“--”，所以这里只能用string接收）
        /// </summary>
        public object Next_exp
        {
            get { return _next_exp; }
            set
            {
                bool isLong = long.TryParse(value.ToString(), out long exp);
                if (isLong) { _next_exp = exp; }
                else _next_exp = long.MinValue;
            }
        }

        /// <summary>
        /// 获取下一升级经验值
        /// </summary>
        /// <returns></returns>
        public long GetNext_expLong()
        {
            if (Current_level == 6) return long.MaxValue;
            else return _next_exp;
        }
    }

    /// <summary>
    /// 钱包
    /// </summary>
    public class Wallet
    {
        //public long Mid { get; set; }

        //public int Bcoin_balance { get; set; }

        public decimal Coupon_balance { get; set; }

        //public int Coupon_due_time { get; set; }
    }

    public class WbiImg
    {
        /// <summary>
        /// https://i0.hdslb.com/bfs/wbi/9cd4224d4fe74c7e9d6963e2ef891688.png
        /// </summary>
        public string img_url { get; set; }

        /// <summary>
        /// https://i0.hdslb.com/bfs/wbi/263655ae2cad4cce95c9c401981b044a.png
        /// </summary>
        public string sub_url { get; set; }

        public string GetImgKey()
        {
            return img_url.Split("wbi/").ToList().Last().Replace(".png", "");
        }

        public string GetSubKey()
        {
            return sub_url.Split("wbi/").ToList().Last().Replace(".png", "");
        }
    }
}
