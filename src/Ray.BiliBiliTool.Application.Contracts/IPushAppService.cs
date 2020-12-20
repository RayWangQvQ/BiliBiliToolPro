using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.Application.Contracts
{
    public interface IPushAppService : IAppService
    {
        void Push();
    }
}
