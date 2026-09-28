using Microsoft.AspNetCore.Components;
using MudBlazor;
using Ray.BiliBiliTool.Web.Services;
using Ray.BiliBiliTool.Web.Services.Pages.BiliAccount;

namespace Ray.BiliBiliTool.Web.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    private IBiliAccountPageWorkflow AccountWorkflow { get; set; } = null!;

    [Inject]
    private ITodayTaskService TodayTaskService { get; set; } = null!;

    private bool _loading = true;
    private int _accountCount;
    private int _doneCount;
    private int _totalCount;

    /// <summary>账号数；未配置账号时显示占位符「—」。</summary>
    public string AccountCountDisplay => _accountCount > 0 ? _accountCount.ToString() : "-";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var accounts = await AccountWorkflow.GetAllAccountsAsync();
            _accountCount = accounts.Count;

            // 本地数据即可（includeBili:false），不发任何网络请求，首屏秒开
            var status = await TodayTaskService.GetTodayStatusAsync(includeBili: false);
            foreach (var account in status)
            {
                foreach (var group in account.Groups)
                {
                    foreach (var item in group.Items)
                    {
                        // 本日无需执行 / 已关闭：既不计入分母也不计入完成
                        if (
                            item.State is TodayTaskItemState.NotToday or TodayTaskItemState.Disabled
                        )
                        {
                            continue;
                        }

                        _totalCount++;
                        if (item.State == TodayTaskItemState.Completed)
                        {
                            _doneCount++;
                        }
                    }
                }
            }
        }
        catch
        {
            // 任一服务失败都保持默认占位（「—」），不阻断首页渲染
        }
        finally
        {
            _loading = false;
        }
    }
}
