using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Ray.BiliBiliTool.Infrastructure
{
    /// <summary>
    /// System.Text.Json的序列化OptionsBuilder
    /// </summary>
    public sealed class JsonSerializerOptionsBuilder
    {
        static JsonSerializerOptionsBuilder()
        {
            DefaultOptions = JsonSerializerOptionsBuilder.Create()
                .GetOrBuildDefaultOptions();
        }

        /// <summary>
        /// 默认配置
        /// </summary>
        public static JsonSerializerOptions DefaultOptions;

        public List<Action<JsonSerializerOptions>> BuildActionList { get; }

        private JsonSerializerOptionsBuilder()
        {
            BuildActionList = new List<Action<JsonSerializerOptions>>();
        }

        public static JsonSerializerOptionsBuilder Create()
        {
            return new JsonSerializerOptionsBuilder();
        }

        public JsonSerializerOptions Build()
        {
            JsonSerializerOptions options = new();//这里没有使用 JsonSerializerDefaults.General 避免后续版本更新后设置改变

            foreach (Action<JsonSerializerOptions> item in BuildActionList)
            {
                item?.Invoke(options);
            }

            return options;
        }

        //适合net5.0以下的情况
        private static JsonSerializerOptions GetJsonOptionsByReflection()
        {
            /*
            * 全局设置默认的序列化配置：驼峰式、支持中文
            * 目前System.Text.Json不支持设置默认Options，这里用反射实现了，以后.net5中可能会新增默认options的接口
            * 详情可参考issue： https://github.com/dotnet/runtime/issues/31094
            */

            JsonSerializerOptions jsonSerializerOptions = (JsonSerializerOptions)typeof(JsonSerializerOptions)
                .GetField(
                    "s_defaultOptions",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .GetValue(null);

            jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

            return jsonSerializerOptions;
        }
    }
}
