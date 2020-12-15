using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Ray.BiliBiliTool.Infrastructure
{
    public static class JsonSerializerOptionsBuilderExtesions
    {
        private static JsonSerializerOptionsBuilder SetActionBase(
            [NotNull] JsonSerializerOptionsBuilder builder,
            [NotNull] Action<JsonSerializerOptions> action)
        {
            builder.CheckNullWithException(nameof(builder));
            action.CheckNullWithException(nameof(action));
            builder.BuildActionList.Add(action);
            return builder;
        }

        #region 设置区

        public static JsonSerializerOptionsBuilder SetEncoder(
            this JsonSerializerOptionsBuilder builder,
            [NotNull] JavaScriptEncoder encoder)
        {
            encoder.CheckNullWithException(nameof(encoder));
            return SetActionBase(
                builder,
                t => t.Encoder = encoder);
        }

        public static JsonSerializerOptionsBuilder SetCamelCase(this JsonSerializerOptionsBuilder builder)
        {
            return SetActionBase(
                builder,
                t => t.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        }

        public static JsonSerializerOptionsBuilder SetEncoderToUnicodeRangeAll(this JsonSerializerOptionsBuilder builder)
        {
            return SetActionBase(
                builder,
                t => t.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All));
        }

        public static JsonSerializerOptionsBuilder Configure(
            this JsonSerializerOptionsBuilder builder,
            [NotNull] Action<JsonSerializerOptions> action)
        {
            return SetActionBase(builder, action);
        }

        #endregion 设置区

        private static JsonSerializerOptions DefaultOptions;

        public static JsonSerializerOptions BuildAndSaveToDefault(
          this JsonSerializerOptionsBuilder builder)
        {
            JsonSerializerOptions option = builder.Build();
            JsonSerializerOptionsBuilderExtesions.DefaultOptions = option;
            return option;
        }

        public static JsonSerializerOptions GetDefaultOptions(this JsonSerializerOptionsBuilder builder)
        {
            return JsonSerializerOptionsBuilderExtesions.DefaultOptions;
        }

        public static JsonSerializerOptions GetOrBuildDefaultOptions(this JsonSerializerOptionsBuilder builder)
        {
            if (JsonSerializerOptionsBuilderExtesions.DefaultOptions.IsNull())
            {
                return builder
                      .SetCamelCase()
                      .SetEncoderToUnicodeRangeAll()
                      .BuildAndSaveToDefault();
            }
            else
            {
                return JsonSerializerOptionsBuilderExtesions.DefaultOptions;
            }
        }
    }
}
