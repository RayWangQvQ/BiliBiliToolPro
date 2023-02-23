using System;
using System.Collections.Generic;
using System.Linq;

namespace Ray.BiliBiliTool.Infrastructure.Cookie
{
    public class CookieInfo
    {
        private readonly CookieStrFactory _ckFactory;

        public CookieInfo(CookieStrFactory ckFactory)
        {
            _ckFactory = ckFactory;
        }

        public string CookieStr => _ckFactory.GetCurrentCookieStr();

        public Dictionary<string, string> CookieItemDictionary => _ckFactory.GetCurrentCookieDic();

        public virtual void Check()
        {
            if (string.IsNullOrWhiteSpace(CookieStr)) throw new Exception("Cookie字符串为空");
        }

        public virtual string CkNameBuild(string name) { return name; }

        public virtual string CkValueBuild(string value) { return value; }

        public override string ToString()
        {
            var list= CookieItemDictionary.Select(d => $"{CkNameBuild(d.Key)}={CkValueBuild(d.Value)}");
            return string.Join("; ", list);
        }

        #region merge

        public void MergeCurrentCookie(string ckStr)
        {
            _ckFactory.MergeCurrentCookie(ckStr);
        }

        public void MergeCurrentCookie(List<string> ckItemList)
        {
            _ckFactory.MergeCurrentCookie(ckItemList);
        }

        public void MergeCurrentCookie(Dictionary<string, string> ckDic)
        {
            _ckFactory.MergeCurrentCookie(ckDic);
        }

        #endregion

        [Obsolete]
        public static Dictionary<string, string> BuildCookieItemDictionaryByCookieItemList(IEnumerable<string> cookieItemList, Func<string, string> nameBuilder = null, Func<string, string> valueBuilder = null)
        {
            var re = new Dictionary<string, string>();
            foreach (var item in cookieItemList ?? new List<string>())
            {
                var index = item.IndexOf('=');
                if (index == -1) continue;

                var name = item[..index].Trim();
                if (nameBuilder != null) name = nameBuilder(name);

                var value = item[(index+1)..].Trim();
                if (valueBuilder != null) value = valueBuilder(value);

                re.TryAdd(name, value);
            }
            return re;
        }
    }
}
