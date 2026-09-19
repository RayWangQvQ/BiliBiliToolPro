using System.Net.Http.Headers;
using System.Text;

namespace Ray.BiliBiliTool.Agent.HttpClientDelegatingHandlers;

/// <summary>
/// 将表单请求体的参数名还原为首字母小写。
/// Refit 对 [Body(UrlEncoded)] 按 CLR 属性名原样序列化（Aid=...&amp;Csrf=...），而迁移前的 WebApiClientCore 会套用
/// camelCase 命名策略（aid=...&amp;csrf=...）。B 站的参数名区分大小写，大小写不符时直接返回
/// {"code":-400,"message":"请求错误"}，且响应体没有 data 字段。
/// 必须注册在 WridEncryptionDelegatingHandler 之前，否则 w_rid 签名覆盖的 key 与实际发出的 key 不一致。
/// </summary>
public class FormUrlEncodedKeyNormalizingDelegatingHandler : DelegatingHandler
{
    private const string FormUrlEncoded = "application/x-www-form-urlencoded";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (
            request.Content?.Headers.ContentType?.MediaType?.Equals(
                FormUrlEncoded,
                StringComparison.OrdinalIgnoreCase
            ) == true
        )
        {
            string original = await request.Content.ReadAsStringAsync(cancellationToken);
            string normalized = NormalizeKeys(original);
            if (normalized != original)
            {
                // 保留原 Content-Type（含 charset 等参数），只替换 body
                MediaTypeHeaderValue? contentType = request.Content.Headers.ContentType;
                request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(normalized));
                if (contentType != null)
                {
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                        contentType.ToString()
                    );
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// 在原始串上就地改 key，避免解码后再编码带来的转义差异。
    /// </summary>
    private static string NormalizeKeys(string form)
    {
        if (form.Length == 0)
        {
            return form;
        }

        string[] pairs = form.Split('&', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < pairs.Length; i++)
        {
            int separator = pairs[i].IndexOf('=');
            string key = separator < 0 ? pairs[i] : pairs[i][..separator];
            if (key.Length == 0 || char.IsLower(key[0]))
            {
                continue;
            }

            string lowered = char.ToLowerInvariant(key[0]) + key[1..];
            pairs[i] = separator < 0 ? lowered : lowered + pairs[i][separator..];
        }

        return string.Join('&', pairs);
    }
}
