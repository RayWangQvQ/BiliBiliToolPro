namespace Ray.BiliBiliTool.Infrastructure.Helpers;

public class IpHelper
{
    public static string? GetIp()
    {
        try
        {
            var re = new HttpClient().GetAsync("http://api.ipify.org/").Result;
            return re.IsSuccessStatusCode ? re.Content.ReadAsStringAsync().Result : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
