using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog.Debugging;

namespace Ray.Serilog.Sinks.Batched
{
    public abstract class IPushService
    {
        public abstract string Name { get; }

        public virtual HttpResponseMessage PushMessage(string message)
        {
            SelfLog.WriteLine($"开始推送到:{Name}");
            return new HttpResponseMessage();
        }
    }
}
