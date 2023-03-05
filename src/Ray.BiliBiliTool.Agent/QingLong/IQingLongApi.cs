using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ray.BiliBiliTool.Agent.Attributes;
using WebApiClientCore.Attributes;

namespace Ray.BiliBiliTool.Agent.QingLong
{
    [LogFilter]
    public interface IQingLongApi
    {
        [HttpGet("/api/envs")]
        Task<QingLongGenericResponse<List<QingLongEnv>>> GetEnvs(string searchValue, [Header("Authorization")] string token);

        [HttpPost("/api/envs")]
        Task<QingLongGenericResponse<List<QingLongEnv>>> AddEnvs([JsonContent] List<AddQingLongEnv> envs, [Header("Authorization")] string token);

        [HttpPut("/api/envs")]
        Task<QingLongGenericResponse<QingLongEnv>> UpdateEnvs([JsonContent] UpdateQingLongEnv env, [Header("Authorization")] string token);
    }

    public class QingLongGenericResponse<T>
    {
        public int Code { get; set; }

        public T Data { get; set; }
    }


    public class QingLongEnv : UpdateQingLongEnv
    {
        public string timestamp { get; set; }
        public int status { get; set; }
        //public long position { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class AddQingLongEnv
    {
        public string value { get; set; }
        public string name { get; set; }
        public string remarks { get; set; }
    }

    public class UpdateQingLongEnv : AddQingLongEnv
    {
        public long id { get; set; }
    }
}
