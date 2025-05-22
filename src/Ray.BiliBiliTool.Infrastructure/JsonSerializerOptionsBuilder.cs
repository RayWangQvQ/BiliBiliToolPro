using System.Text.Json;

namespace Ray.BiliBiliTool.Infrastructure;

/// <summary>
/// System.Text.Json的序列化OptionsBuilder
/// </summary>
public sealed class JsonSerializerOptionsBuilder
{
    static JsonSerializerOptionsBuilder()
    {
        DefaultOptions = Create().GetOrBuildDefaultOptions();
    }

    /// <summary>
    /// 默认配置
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions;

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
        JsonSerializerOptions options = new(); //这里没有使用 JsonSerializerDefaults.General 避免后续版本更新后设置改变

        foreach (Action<JsonSerializerOptions> item in BuildActionList)
        {
            item?.Invoke(options);
        }

        return options;
    }
}
