using System;
using System.Collections.Generic;
using System.Text;
using Ray.BiliBiliTool.Agent.Dtos;

namespace Ray.BiliBiliTool.DomainService.Interfaces
{
    public interface IVideoDomainService : IDomainService
    {
        string GetRandomVideo();

        void WatchVideo(string aid, DailyTaskInfo dailyTaskStatus);

        void ShareVideo(string aid, DailyTaskInfo dailyTaskStatus);

        void AddCoinsForVideo();

        bool AddCoinsForVideo(string aid, int multiply, int select_like);

        bool IsDonatedCoinsForVideo(string aid);
    }
}
