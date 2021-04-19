using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Infrastructure
{
    public class CookieInfo
    {
        public CookieInfo(string cookieStr)
        {
            CookieStr = cookieStr ?? "";

            CookieItemList = CookieStr.Split(";")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            foreach (var item in CookieItemList)
            {
                var list = item.Split('=');
                if (list.Length >= 2)
                    CookieItemDictionary.TryAdd(list[0].Trim(), list[1].Trim());
            }
        }

        public string CookieStr { get; set; }

        public List<string> CookieItemList { get; set; }

        public Dictionary<string, string> CookieItemDictionary { get; set; } = new Dictionary<string, string>();

        public virtual CookieContainer CreateCookieContainer(Uri uri)
        {
            var cookieContainer = new CookieContainer();
            foreach (var item in CookieItemList)
            {
                cookieContainer.SetCookies(uri, item);
            }

            return cookieContainer;
        }

        public virtual void Check()
        {
            if (string.IsNullOrWhiteSpace(CookieStr)) throw new Exception("Cookie字符串为空");
        }
    }
}
