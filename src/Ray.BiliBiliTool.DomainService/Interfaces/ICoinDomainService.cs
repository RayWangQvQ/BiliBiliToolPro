using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface ICoinDomainService : IDomainService
    {
        int GetCoinBalance();

        int GetDonatedCoins();
    }
}
