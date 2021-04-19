using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Ray.BiliBiliTool.Infrastructure
{
    public class CookieStrFactory
    {
        private List<string> _strList = new List<string>();

        public CookieStrFactory(List<string> strList)
        {
            _strList = strList;
        }

        public int CurrentNum { get; set; } = 1;
        private int _index => CurrentNum - 1;

        public int Count => _strList.Count;

        public bool Any()
        {
            if (CurrentNum <= Count) return true;
            else return false;
        }

        public string GetCurrentCookieStr()
        {
            if (!Any()) throw new Exception($"第 {CurrentNum} 个cookie字符串不存在");

            return _strList[_index];
        }
    }
}
