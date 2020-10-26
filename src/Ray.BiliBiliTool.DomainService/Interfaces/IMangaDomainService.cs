using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface IMangaDomainService : IDomainService
    {
        void MangaSign();

        void ReceiveMangaVipReward(int reason_id, UseInfo userIfo);
    }
}
