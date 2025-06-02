using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Ray.BiliBiliTool.Infrastructure;

public static class JsonSerializerOptionsBuilderExtensions
{
    private static JsonSerializerOptionsBuilder SetActionBase(
        JsonSerializerOptionsBuilder builder,
        Action<JsonSerializerOptions> action
    )
    {
        builder.CheckNullWithException(nameof(builder));
        action.CheckNullWithException(nameof(action));
        builder.BuildActionList.Add(action);
        return builder;
    }

    #region 设置区

    public static JsonSerializerOptionsBuilder SetEncoder(
        this JsonSerializerOptionsBuilder builder,
        JavaScriptEncoder encoder
    )
    {
        encoder.CheckNullWithException(nameof(encoder));
        return SetActionBase(builder, t => t.Encoder = encoder);
    }

    public static JsonSerializerOptionsBuilder SetCamelCase(
        this JsonSerializerOptionsBuilder builder
    )
    {
        return SetActionBase(builder, t => t.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
    }

    public static JsonSerializerOptionsBuilder SetEncoderToUnicodeRangeAll(
        this JsonSerializerOptionsBuilder builder
    )
    {
        return SetActionBase(builder, t => t.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All));
    }

    public static JsonSerializerOptionsBuilder Configure(
        this JsonSerializerOptionsBuilder builder,
        Action<JsonSerializerOptions> action
    )
    {
        return SetActionBase(builder, action);
    }

    #endregion 设置区

    private static JsonSerializerOptions? _defaultOptions;

    private static JsonSerializerOptions BuildAndSaveToDefault(
        this JsonSerializerOptionsBuilder builder
    )
    {
        JsonSerializerOptions option = builder.Build();
        _defaultOptions = option;
        return option;
    }

    public static JsonSerializerOptions? GetDefaultOptions(
        this JsonSerializerOptionsBuilder builder
    )
    {
        return _defaultOptions;
    }

    public static JsonSerializerOptions GetOrBuildDefaultOptions(
        this JsonSerializerOptionsBuilder builder
    )
    {
        return _defaultOptions.IsNull()
            ? builder.SetCamelCase().SetEncoderToUnicodeRangeAll().BuildAndSaveToDefault()
            : _defaultOptions!;
    }
}
