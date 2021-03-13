using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Infrastructure.Helpers
{
    public class RandomHelper
    {
        private int rep = 0;

        public string GenerateCode(int codeCount)
        {
            string str = string.Empty;
            long num2 = DateTime.Now.Ticks + this.rep;
            this.rep++;
            Random random = new Random(
                                (int)((ulong)num2 & 0xffffffL) | (int)(num2 >> this.rep)
                            );
            for (int i = 0; i < codeCount; i++)
            {
                char ch;
                int num = random.Next();
                if ((num % 2) == 0)
                {
                    ch = (char)(0x30 + ((ushort)(num % 10)));
                }
                else
                {
                    ch = (char)(0x41 + ((ushort)(num % 0x1a)));
                }

                str += ch.ToString();
            }

            return str;
        }
    }
}
