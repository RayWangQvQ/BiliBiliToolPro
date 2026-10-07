using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Ray.BiliBiliTool.Web.Components.Comps;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

public class ScheduleTimePickerTests : TestContext
{
    public ScheduleTimePickerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void OpeningExistingSchedule_DoesNotRewriteIt()
    {
        var changes = new List<string?>();
        var component = RenderComponent<ScheduleTimePicker>(p =>
            p.Add(x => x.Value, "0 0 12 28 * ?")
                .Add(x => x.ValueChanged, (string? value) => changes.Add(value))
        );
        Assert.Equal(
            "Monthly",
            component.Find("select[aria-label=执行周期]").GetAttribute("value")
        );
        Assert.Equal("12", component.Find("select[aria-label=小时]").GetAttribute("value"));
        Assert.Equal("0", component.Find("select[aria-label=分钟]").GetAttribute("value"));
        Assert.Contains("每月 28 日 12:00", component.Markup);
        Assert.Empty(changes);
    }

    [Fact]
    public void TimeSelection_UpdatesBoundCronAndSavedForm()
    {
        var form = RenderComponent<SchedulePickerForm>();
        form.Find("select[aria-label=小时]").Change("8");
        form.Find("select[aria-label=分钟]").Change("30");
        Assert.Equal("0 30 8 * * ?", form.Instance.Settings.Cron);
        form.Find("form").Submit();
        Assert.Equal(1, form.Instance.SaveCount);
    }

    [Fact]
    public void InvalidHour_BlocksSavingUntilCorrected()
    {
        var form = RenderComponent<SchedulePickerForm>();
        form.Find("select[aria-label=小时]").Change("");
        form.Find("form").Submit();
        Assert.Equal(0, form.Instance.SaveCount);
        Assert.Contains("请选择执行时间", form.Markup);
        form.Find("select[aria-label=小时]").Change("9");
        form.Find("select[aria-label=分钟]").Change("10");
        form.Find("form").Submit();
        Assert.Equal(1, form.Instance.SaveCount);
        Assert.Equal("0 10 9 * * ?", form.Instance.Settings.Cron);
    }

    [Fact]
    public void WeeklySelection_RequiresAtLeastOneDay()
    {
        var form = RenderComponent<SchedulePickerForm>();
        form.Find("select[aria-label=执行周期]").Change("Weekly");
        form.FindAll("input[type=checkbox]")[0].Change(false);
        form.Find("form").Submit();
        Assert.Equal(0, form.Instance.SaveCount);
        Assert.Contains("请至少选择一天", form.Markup);
        form.FindAll("input[type=checkbox]")[2].Change(true);
        form.Find("form").Submit();
        Assert.Equal(1, form.Instance.SaveCount);
        Assert.Equal("0 0 15 ? * WED", form.Instance.Settings.Cron);
    }

    [Fact]
    public void SpecialPlan_IsPreservedUntilExplicitlyReplaced()
    {
        var form = RenderComponent<SchedulePickerForm>(p =>
            p.Add(x => x.InitialCron, "0 0 8 ? * MON#2")
        );
        Assert.Empty(form.FindAll("select[aria-label=小时]"));
        form.Find("form").Submit();
        Assert.Equal("0 0 8 ? * MON#2", form.Instance.Settings.Cron);
        form.Find("button[type=button]").Click();
        Assert.Equal("0 0 8 * * ?", form.Instance.Settings.Cron);
        Assert.Single(form.FindAll("select[aria-label=小时]"));
    }

    [Fact]
    public void DisabledTask_DoesNotAcceptScheduleChanges()
    {
        var form = RenderComponent<SchedulePickerForm>(p => p.Add(x => x.Disabled, true));
        Assert.True(form.Find("fieldset").HasAttribute("disabled"));
        form.Find("select[aria-label=小时]").Change("10");
        form.Find("select[aria-label=分钟]").Change("0");
        Assert.Equal("0 0 15 * * ?", form.Instance.Settings.Cron);
    }

    [Fact]
    public void MonthlyAndIntervalControls_GenerateSchedules()
    {
        var form = RenderComponent<SchedulePickerForm>();
        form.Find("select[aria-label=执行周期]").Change("Monthly");
        form.Find("select[aria-label=日期]").Change("0");
        Assert.Equal("0 0 15 L * ?", form.Instance.Settings.Cron);
        form.Find("select[aria-label=执行周期]").Change("Hours");
        form.Find("select[aria-label=执行间隔]").Change("6");
        form.Find("select[aria-label=执行分钟]").Change("20");
        Assert.Equal("0 20 0/6 * * ?", form.Instance.Settings.Cron);
        form.Find("select[aria-label=执行周期]").Change("Minutes");
        form.Find("select[aria-label=执行间隔]").Change("15");
        Assert.Equal("0 0/15 * * * ?", form.Instance.Settings.Cron);
    }

    [Fact]
    public void YearlyMonthChange_KeepsDayValid()
    {
        var form = RenderComponent<SchedulePickerForm>(p =>
            p.Add(x => x.InitialCron, "0 0 8 31 1 ?")
        );
        form.Find("select[aria-label=月份]").Change("2");
        Assert.Equal("0 0 8 29 2 ?", form.Instance.Settings.Cron);
        Assert.Contains("在闰年执行", form.Markup);
    }
}
