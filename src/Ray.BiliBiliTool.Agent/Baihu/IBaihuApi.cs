using Ray.BiliBiliTool.Agent.Baihu.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.Baihu;

public interface IBaihuApi
{
    [Get("/open2api/v1/env/all")]
    Task<BaihuGenericResponse<List<BaihuEnv>>> GetEnvsAsync(
        [Header("Authorization")] string token
    );

    [Post("/open2api/v1/env")]
    Task<BaihuGenericResponse<BaihuEnv>> AddEnvAsync(
        [Body] BaihuEnv env,
        [Header("Authorization")] string token
    );

    [Put("/open2api/v1/env/{id}")]
    Task<BaihuGenericResponse<BaihuEnv>> UpdateEnvAsync(
        string id,
        [Body] BaihuEnv env,
        [Header("Authorization")] string token
    );
}
