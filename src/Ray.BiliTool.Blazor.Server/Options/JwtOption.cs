namespace Ray.BiliTool.Blazor.Server.Options
{
    public class JwtOption
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiryInHours { get; set; }
    }
}
