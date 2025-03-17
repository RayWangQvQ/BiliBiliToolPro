using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Ray.BiliBiliTool.Application.Contracts;

public static class TaskTypeFactory
{
    private static readonly List<Type> TypeList =
    [
        typeof(ILoginTaskAppService),
        typeof(ITestAppService),
        typeof(IDailyTaskAppService),
        typeof(ILiveFansMedalAppService),
        typeof(ILiveLotteryTaskAppService),
        typeof(IVipBigPointAppService),
        typeof(IUnfollowBatchedTaskAppService),
    ];

    private static readonly List<TaskTypeItem> All = [];

    static TaskTypeFactory()
    {
        for (int i = 0; i < TypeList.Count; i++)
        {
            All.Add(
                new TaskTypeItem(
                    i + 1,
                    TypeList[i].GetCustomAttribute<DescriptionAttribute>()?.Description,
                    TypeList[i]
                )
            );
        }
    }

    public static Type Get(string code)
    {
        return All.First(x => x.Code == code).Type;
    }

    public static void Show(ILogger logger)
    {
        foreach (var item in All)
        {
            logger.LogInformation("{id}):{code}", item.Id, item.Code);
        }
    }

    public static string GetCodeByIndex(int index)
    {
        return All.First(x => x.Id == index).Code;
    }
}

public class TaskTypeItem(int id, string code, Type type)
{
    public int Id { get; } = id;

    public string Code { get; } = code;

    public Type Type { get; } = type;
}
