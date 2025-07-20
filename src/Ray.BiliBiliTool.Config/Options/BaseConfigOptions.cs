namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// 基础配置选项类，包含所有配置共有的属性
/// </summary>
public abstract class BaseConfigOptions : IHasCron, IConfigOptions
{
    /// <summary>
    /// 定时任务Cron表达式
    /// </summary>
    public string? Cron { get; set; }

    /// <summary>
    /// 是否启用该任务
    /// </summary>
    public bool IsEnable { get; set; } = true;

    /// <summary>
    /// 配置节名称，由子类实现
    /// </summary>
    public abstract string SectionName { get; }

    /// <summary>
    /// 转换为配置字典，子类可以重写以添加更多配置项
    /// </summary>
    public virtual Dictionary<string, string> ToConfigDictionary()
    {
        return GetBaseConfigDictionary();
    }

    /// <summary>
    /// 获取基础配置字典，避免循环调用
    /// </summary>
    protected Dictionary<string, string> GetBaseConfigDictionary()
    {
        return new Dictionary<string, string>
        {
            { $"{SectionName}:{nameof(Cron)}", Cron ?? "" },
            { $"{SectionName}:{nameof(IsEnable)}", IsEnable.ToString().ToLower() },
        };
    }

    /// <summary>
    /// 合并配置字典，用于子类添加额外配置项
    /// </summary>
    protected Dictionary<string, string> MergeConfigDictionary(
        Dictionary<string, string> additionalConfig
    )
    {
        var baseConfig = GetBaseConfigDictionary();
        foreach (var kvp in additionalConfig)
        {
            baseConfig[kvp.Key] = kvp.Value;
        }
        return baseConfig;
    }
}
