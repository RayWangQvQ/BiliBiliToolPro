using System;
using System.Collections.Generic;
using System.Text;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface IVideoDomainService : IDomainService
    {
        string GetRandomVideo();

        void WatchVideo(string aid);

        void ShareVideo(string aid);

        void AddCoinsForVideo();

        bool AddCoinsForVideo(string aid, int multiply, int select_like);

        bool IsDonatedCoinsForVideo(string aid);
    }
}
