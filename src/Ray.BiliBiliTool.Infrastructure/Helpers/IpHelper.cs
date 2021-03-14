using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Infrastructure.Helpers
{
    public class IpHelper
    {
        public static string GetIp()
        {
            try
            {
                var re = new HttpClient()
                    .GetAsync("http://api.ipify.org/")
                    .Result;
                return re.IsSuccessStatusCode
                    ? re.Content?.ReadAsStringAsync()?.Result
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
