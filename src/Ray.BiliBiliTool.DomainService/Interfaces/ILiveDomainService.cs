using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface ILiveDomainService : IDomainService
    {
        void LiveSign();
        int ExchangeSilver2Coin();
    }
}
