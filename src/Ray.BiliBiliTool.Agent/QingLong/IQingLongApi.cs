using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using Refit;

namespace Ray.BiliBiliTool.Agent.QingLong;

public interface IQingLongApi
{
    [Get("/open/auth/token")]
    Task<QingLongGenericResponse<TokenResponse>> GetTokenAsync(
        [Query] string client_id,
        [Query] string client_secret
    );

    [Get("/open/envs")]
    Task<QingLongGenericResponse<List<QingLongEnv>>> GetEnvsAsync(
        [Query] string searchValue,
        [Header("Authorization")] string token
    );

    [Post("/open/envs")]
    Task<QingLongGenericResponse<List<QingLongEnv>>> AddEnvsAsync(
        [Body] List<AddQingLongEnv> envs,
        [Header("Authorization")] string token
    );

    [Put("/open/envs")]
    Task<QingLongGenericResponse<QingLongEnv>> UpdateEnvsAsync(
        [Body] UpdateQingLongEnv env,
        [Header("Authorization")] string token
    );
}
