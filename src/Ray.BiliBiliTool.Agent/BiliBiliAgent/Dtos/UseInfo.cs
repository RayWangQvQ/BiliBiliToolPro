using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    /// <summary>
    /// 账户信息
    /// </summary>
    public class UseInfo
    {
        public bool IsLogin { get; set; }

        public LevelInfo Level_info { get; set; }

        public decimal? Money { get; set; }

        public string Uname { get; set; }

        public Wallet Wallet { get; set; }

        public int VipStatus { get; set; }

        public int VipType { get; set; }//todo:是否可以改为枚举

        /// <summary>
        /// 获取隐私处理后的用户名
        /// </summary>
        /// <returns></returns>
        public string GetFuzzyUname()
        {
            StringBuilder sb = new StringBuilder();
            int s1 = Uname.Length / 2, s2 = (s1 + 1) / 2;
            for (int i = 0; i < Uname.Length; i++)
            {
                if (i >= s2 && i < s1 + s2) sb.Append("*");
                else sb.Append(Uname[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 返回会员类型
        /// </summary>
        /// <returns>0:无会员（会员过期、当前不是会员）;
        /// 1:月会员
        /// 2:年会员</returns>
        public int GetVipType()
        {
            if (this.VipStatus == 1)
            {
                //只有VipStatus为1的时候获取到VipType才是有效的。
                return this.VipType;
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// 会员等级
    /// </summary>
    public class LevelInfo
    {
        public int Current_level { get; set; }

        //public long Current_min { get; set; }

        /// <summary>
        /// 当前经验值
        /// </summary>
        public long Current_exp { get; set; }

        private string _next_exp;
        /// <summary>
        /// 下一升级经验值（因为Lv6的大佬会返回字符串“--”，所以这里只能用string接收）
        /// </summary>
        public object Next_exp
        {
            get { return _next_exp; }
            set { _next_exp = value.ToString(); }
        }

        public long GetNext_expLong()
        {
            if (Current_level == 6) return long.MaxValue;
            if (long.TryParse(Next_exp.ToString(), out long result)) return result;
            return long.MinValue;
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
}
