using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ray.BiliBiliTool.Agent.Push
{
    public interface IPushService
    {
        string Name { get; }

        bool Send(string title, string content);
    }
}
