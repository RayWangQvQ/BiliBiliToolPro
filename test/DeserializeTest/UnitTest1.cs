using System.Diagnostics;
using System.Text.Json;
using System.Text.Unicode;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;
using Xunit;

namespace DeserializeTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            string s = "{\"code\":0,\"message\":\"0\",\"ttl\":1,\"data\":{\"isLogin\":true,\"email_verified\":1,\"face\":\"\",\"level_info\":{\"current_level\":4,\"current_min\":4500,\"current_exp\":8532,\"next_exp\":\"--\"},\"mid\":220893216,\"mobile_verified\":1,\"money\":856.5,\"moral\":70,\"official\":{\"role\":0,\"title\":\"\",\"desc\":\"\",\"type\":-1},\"officialVerify\":{\"type\":-1,\"desc\":\"\"},\"pendant\":{\"pid\":0,\"name\":\"\",\"image\":\"\",\"expire\":0,\"image_enhance\":\"\"},\"scores\":0,\"uname\":\"在7楼\",\"vipDueDate\":1613404800000,\"vipStatus\":1,\"vipType\":2,\"vip_pay_type\":0,\"vip_theme_type\":0,\"vip_label\":{\"path\":\"\",\"text\":\"年度大会员\",\"label_theme\":\"annual_vip\"},\"vip_avatar_subscript\":1,\"vip_nickname_color\":\"#FB7299\",\"wallet\":{\"mid\":220893216,\"bcoin_balance\":0,\"coupon_balance\":0,\"coupon_due_time\":0},\"has_shop\":false,\"shop_url\":\"\",\"allowance_count\":0,\"answer_status\":0}}";

            //需要指定为驼峰式
            JsonSerializerOptions defaultOptions = (JsonSerializerOptions)typeof(JsonSerializerOptions)
                .GetField("s_defaultOptions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null);
            defaultOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            defaultOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All);

            var re = JsonSerializer.Deserialize<BiliApiResponse<UseInfo>>(s);

            Debug.WriteLine(JsonSerializer.Serialize(re));
            Assert.NotNull(re);
        }
    }
}
