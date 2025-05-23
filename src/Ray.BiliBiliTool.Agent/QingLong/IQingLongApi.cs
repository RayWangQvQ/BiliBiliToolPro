using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.QingLong;

[LogFilter]
public interface IQingLongApi
{
    [HttpGet("/open/auth/token")]
    Task<QingLongGenericResponse<TokenResponse>> GetTokenAsync(
        string client_id,
        string client_secret
    );

    [HttpGet("/open/envs")]
    Task<QingLongGenericResponse<List<QingLongEnv>>> GetEnvsAsync(
        string searchValue,
        [Header("Authorization")] string token
    );

    [HttpPost("/open/envs")]
    Task<QingLongGenericResponse<List<QingLongEnv>>> AddEnvsAsync(
        [JsonContent] List<AddQingLongEnv> envs,
        [Header("Authorization")] string token
    );

    [HttpPut("/open/envs")]
    Task<QingLongGenericResponse<QingLongEnv>> UpdateEnvsAsync(
        [JsonContent] UpdateQingLongEnv env,
        [Header("Authorization")] string token
    );
}
