using System;
using System.Text;
using System.Text.Json;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.Extensions.Logging;
using Quartz;
using static System.Net.Mime.MediaTypeNames;

namespace BlazingQuartz.Jobs
{
    public class HttpJob : IJob
    {
        public const string PropertyRequestAction = "requestAction";
        public const string PropertyRequestUrl = "requestUrl";
        public const string PropertyRequestParameters = "requestParams";
        public const string PropertyRequestHeaders = "requestHeaders";
        public const string PropertyIgnoreVerifySsl = "ignoreSsl";

        /// <summary>
        /// HTTP request timeout. Negative value to indicate infinite timeout.
        /// </summary>
        public const string PropertyRequestTimeoutInSec = "requestTimeout";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpJob> _logger;
        private readonly IDataMapValueResolver _dmvResolver;

        public HttpJob(
            IHttpClientFactory httpClientFactory,
            ILogger<HttpJob> logger,
            IDataMapValueResolver dmvResolver
        )
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _dmvResolver = dmvResolver;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var data = context.MergedJobDataMap;

                int? timeoutInSec = data.TryGetInt(PropertyRequestTimeoutInSec, out var x)
                    ? x
                    : null;
                var dmvUrl = data.GetDataMapValue(PropertyRequestUrl);
                var url = _dmvResolver.Resolve(dmvUrl);
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning(
                        "[{runInstanceId}]. Cannot run HttpJob. No request url specified.",
                        context.FireInstanceId
                    );
                    throw new JobExecutionException("No request url specified");
                }
                url = url.StartsWith("http") ? url : "http://" + url;

                var parameters = _dmvResolver.Resolve(
                    data.GetDataMapValue(PropertyRequestParameters)
                );
                var strHeaders = _dmvResolver.Resolve(data.GetDataMapValue(PropertyRequestHeaders));
                var headers = string.IsNullOrEmpty(strHeaders)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(strHeaders.Trim());

                var strAction = data.GetString(PropertyRequestAction);
                HttpAction action;
                if (strAction == null)
                {
                    _logger.LogWarning(
                        "[{runInstanceId}]. Cannot run HttpJob. No http action specified.",
                        context.FireInstanceId
                    );
                    throw new JobExecutionException("No http action specified");
                }
                action = Enum.Parse<HttpAction>(strAction);

                _logger.LogDebug(
                    "[{runInstanceId}]. Creating HttpClient...",
                    context.FireInstanceId
                );
                HttpClient httpClient;
                if (
                    data.TryGetBoolean(PropertyIgnoreVerifySsl, out var IgnoreVerifySsl)
                    && IgnoreVerifySsl
                )
                {
                    httpClient = _httpClientFactory.CreateClient(
                        Constants.HttpClientIgnoreVerifySsl
                    );
                    _logger.LogInformation(
                        "[{runInstanceId}]. Created ignore SSL validation HttpClient.",
                        context.FireInstanceId
                    );
                }
                else
                {
                    httpClient = _httpClientFactory.CreateClient();
                    _logger.LogInformation(
                        "[{runInstanceId}]. Created HttpClient.",
                        context.FireInstanceId
                    );
                }

                // configure time out. Default 100 secs
                if (timeoutInSec.HasValue)
                {
                    if (timeoutInSec > 0)
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(timeoutInSec.Value);
                    }
                    else
                    {
                        httpClient.Timeout = Timeout.InfiniteTimeSpan;
                    }
                }

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                HttpContent? reqParam = null;
                if (!string.IsNullOrEmpty(parameters))
                    reqParam = new StringContent(parameters, Encoding.UTF8, Application.Json);

                HttpResponseMessage response = new HttpResponseMessage();
                _logger.LogInformation(
                    "[{runInstanceId}]. Sending '{action}' request to specified url '{url}'.",
                    context.FireInstanceId,
                    action,
                    url
                );
                switch (action)
                {
                    case HttpAction.Get:
                        response = await httpClient.GetAsync(url, context.CancellationToken);
                        break;
                    case HttpAction.Post:
                        response = await httpClient.PostAsync(
                            url,
                            reqParam,
                            context.CancellationToken
                        );
                        break;
                    case HttpAction.Put:
                        response = await httpClient.PutAsync(
                            url,
                            reqParam,
                            context.CancellationToken
                        );
                        break;
                    case HttpAction.Delete:
                        response = await httpClient.DeleteAsync(url, context.CancellationToken);
                        break;
                }

                var result = await response.Content.ReadAsStringAsync(context.CancellationToken);
                _logger.LogInformation(
                    "[{runInstanceId}]. Response tatus code '{code}'.",
                    context.FireInstanceId,
                    response.StatusCode
                );
                context.Result = result;
                context.SetIsSuccess(response.IsSuccessStatusCode);
                context.SetReturnCode((int)response.StatusCode);
                context.SetExecutionDetails($"Request: [{response.RequestMessage}]");
            }
            catch (JobExecutionException)
            {
                context.SetIsSuccess(false);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to run HttpJob. [{runInstanceId}]",
                    context.FireInstanceId
                );
                context.SetIsSuccess(false);
                throw new JobExecutionException("Failed to execute http job", ex);
            }
        }
    }
}
