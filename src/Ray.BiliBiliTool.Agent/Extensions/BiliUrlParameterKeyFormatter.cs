using Refit;

namespace Ray.BiliBiliTool.Agent.Extensions;

/// <summary>
/// 将 query 参数名还原为首字母小写。
/// Refit 按 CLR 属性名原样拼接 query（?Vmid=...&amp;Order_type=...），而迁移前的 WebApiClientCore 会套用
/// camelCase 命名策略（?vmid=...&amp;order_type=...）。B 站的参数名区分大小写，大小写不符时直接返回
/// {"code":-400,"message":"请求错误"}，且响应体没有 data 字段。
/// </summary>
public sealed class BiliUrlParameterKeyFormatter : IUrlParameterKeyFormatter
{
    public string Format(string key) =>
        key.Length == 0 || char.IsLower(key[0]) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}
