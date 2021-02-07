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

            CookieStrList = CookieStr.Split(";")
                .Select(x => x.Trim())
                .Where(x => x.IsNotNullOrEmpty())
                .ToList();

            foreach (var item in CookieStrList)
            {
                var list = item.Split('=');
                CookieDictionary.TryAdd(list[0].Trim(), list[1].Trim());
            }
        }

        public string CookieStr { get; set; }

        public List<string> CookieStrList { get; set; }

        public Dictionary<string, string> CookieDictionary { get; set; } = new Dictionary<string, string>();

        public virtual CookieContainer CreateCookieContainer(Uri uri)
        {
            var cookieContainer = new CookieContainer();
            foreach (var item in CookieStrList)
            {
                cookieContainer.SetCookies(uri, item);
            }

            return cookieContainer;
        }
    }
}
