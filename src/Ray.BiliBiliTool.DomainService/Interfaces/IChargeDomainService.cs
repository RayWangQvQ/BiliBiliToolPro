using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface IChargeDomainService : IDomainService
    {
        void Charge(UseInfo userInfo);

        void ChargeComments(string token);
    }
}
