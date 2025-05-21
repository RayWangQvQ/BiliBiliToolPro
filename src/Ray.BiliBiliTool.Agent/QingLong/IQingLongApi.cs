using System.Collections.Generic;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using Ray.BiliBiliTool.Agent.QingLong.Dtos;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.QingLong;

[LogFilter]
public interface IQingLongApi
{
    [HttpGet("/api/envs")]
    Task<QingLongGenericResponse<List<QingLongEnv>>> GetEnvs(
        string searchValue,
        [Header("Authorization")] string token
    );

    [HttpPost("/api/envs")]
    Task<QingLongGenericResponse<List<QingLongEnv>>> AddEnvs(
        [JsonContent] List<AddQingLongEnv> envs,
        [Header("Authorization")] string token
    );

    [HttpPut("/api/envs")]
    Task<QingLongGenericResponse<QingLongEnv>> UpdateEnvs(
        [JsonContent] UpdateQingLongEnv env,
        [Header("Authorization")] string token
    );
}
