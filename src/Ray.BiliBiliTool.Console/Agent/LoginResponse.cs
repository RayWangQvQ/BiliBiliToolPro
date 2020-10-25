using System;
using System.Collections.Generic;
using System.Text;

namespace BiliBiliTool.Agent
{
    public class LoginResponse
    {
        public bool IsLogin { get; set; }

        public LevelInfo Level_info { get; set; }

        public long Money { get; set; }

        public string Uname { get; set; }

        public Wallet Wallet { get; set; }

        public int VipStatus { get; set; }

        public int VipType { get; set; }

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
    }

    public class LevelInfo
    {
        public int Current_level { get; set; }

        public long Current_min { get; set; }

        public long Current_exp { get; set; }

        public long Next_exp { get; set; }
    }

    public class Wallet
    {
        public long Mid { get; set; }

        public int Bcoin_balance { get; set; }

        public int Coupon_balance { get; set; }

        public int Coupon_due_time { get; set; }
    }
}
