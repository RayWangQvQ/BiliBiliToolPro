using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ray.Serilog.Sinks.Batched
{
    public interface IPushService
    {
        Task<HttpResponseMessage> PushMessageAsync(string message);
    }
}
