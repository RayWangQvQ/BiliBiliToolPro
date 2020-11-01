using System;
using System.Text.Json;
using System.Text.Unicode;

namespace Ray.BiliBiliTool.Infrastructure
{
    /// <summary>
    /// System.Text.Json的序列化OptionsBuilder
    /// </summary>
    public class JsonSerializerOptionsBuilder
    {
        static JsonSerializerOptionsBuilder()
        {
            /*
             * 全局设置默认的序列化配置：驼峰式、支持中文
             * 目前System.Text.Json不支持设置默认Options，这里用反射实现了，以后.net5中可能会新增默认options的接口
             * 详情可参考issue： https://github.com/dotnet/runtime/issues/31094
             */
            JsonSerializerOptions defaultJsonSerializerOptions = (JsonSerializerOptions)typeof(JsonSerializerOptions)
                .GetField("s_defaultOptions", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null);
            defaultJsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            defaultJsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All);

            DefaultOptions = defaultJsonSerializerOptions;
        }

        /// <summary>
        /// 默认配置
        /// </summary>
        public static JsonSerializerOptions DefaultOptions;

        public static JsonSerializerOptions Builder(Action<JsonSerializerOptions> build)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            build(options);
            return options;
        }
    }
}
