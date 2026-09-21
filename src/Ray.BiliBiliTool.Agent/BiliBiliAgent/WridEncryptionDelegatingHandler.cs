using System.Collections.Specialized;
using System.Web;
using Ray.BiliBiliTool.Agent.BiliBiliAgent.Services;
using Ray.BiliBiliTool.Infrastructure.Cookie;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent;

public class WridEncryptionDelegatingHandler(IWbiService wbiService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (request.Content is FormUrlEncodedContent originalFormContent)
        {
            var originalFormDataString = await originalFormContent.ReadAsStringAsync(
                cancellationToken
            );
            var formData = HttpUtility.ParseQueryString(originalFormDataString);

            await TrySetWridAync(request, formData, cancellationToken);

            var newFormKeyValuePairs = formData
                .AllKeys.Select(key => new KeyValuePair<string, string>(key!, formData[key] ?? ""))
                .ToList();
            request.Content = new FormUrlEncodedContent(newFormKeyValuePairs);
        }

        if (request.RequestUri?.Query != null)
        {
            var queryParameters = HttpUtility.ParseQueryString(request.RequestUri.Query);

            await TrySetWridAync(request, queryParameters, cancellationToken);

            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Query = queryParameters?.ToString() ?? "",
            };
            request.RequestUri = uriBuilder.Uri;
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task TrySetWridAync(
        HttpRequestMessage request,
        NameValueCollection formData,
        CancellationToken cancellationToken
    )
    {
        var paramsToSign = new Dictionary<string, string>();
        foreach (var key in formData.AllKeys)
        {
            paramsToSign[key!] = formData[key] ?? "";
        }

        // 用 wts 判断是否需要签名，不能用 w_rid：
        // w_rid 是可空引用类型，值为 null 时 Refit 会直接把它从查询串里丢掉，
        // 于是这里永远判断为「不需要签名」，请求就变成未签名，B站一律回 -403。
        // wts 是 long（非空值类型），一定会出现在查询串里，可稳定作为 IWrid 的标记。
        if (paramsToSign.All(x => x.Key != "wts"))
        {
            return;
        }

        var ckStr = request
            .Headers.FirstOrDefault(x => x.Key == "Cookie")
            .Value.FirstOrDefault()
            ?.ToString();
        var ck = CookieStrFactory<BiliCookie>.CreateNew(ckStr ?? "");

        var wbi = await wbiService.GetWridAsync(paramsToSign, ck);

        formData["w_rid"] = wbi.w_rid;
        formData["wts"] = wbi.wts.ToString();
    }
}
