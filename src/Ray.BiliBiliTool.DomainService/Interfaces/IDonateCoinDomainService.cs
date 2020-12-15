using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    /// <summary>
    /// 投币
    /// </summary>
    public interface IDonateCoinDomainService : IDomainService
    {
        void AddCoinsForVideo();
    }
}
