using Ray.BiliBiliTool.Agent.DaiDai.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.DaiDai;

/// <summary>
/// 呆呆面板（Daidai Panel）原生 Open API。
/// 鉴权流程：先用 AppKey/AppSecret 换 access_token，再带 Authorization: Bearer {token}
/// 调用环境变量接口（面板里新建的 Open API 应用需具备 envs 授权范围）。
/// </summary>
public interface IDaiDaiApi
{
    [Post("/api/open-api/token")]
    Task<DaiDaiTokenResponse> GetTokenAsync([Body] DaiDaiTokenRequest request);

    [Get("/api/envs")]
    Task<DaiDaiEnvsResponse> GetEnvsAsync(
        string keyword,
        string all,
        [Header("Authorization")] string token
    );

    [Post("/api/envs")]
    Task<DaiDaiEnvResponse> AddEnvAsync([Body] DaiDaiEnv env, [Header("Authorization")] string token);

    [Put("/api/envs/{id}")]
    Task<DaiDaiEnvResponse> UpdateEnvAsync(
        long id,
        [Body] DaiDaiEnv env,
        [Header("Authorization")] string token
    );
}
