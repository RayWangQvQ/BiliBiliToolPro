using BlazingQuartz.Core.Models;
using BlazingQuartz.Core.Services;
using BlazingQuartz.Jobs.Abstractions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Components.Comps;

public partial class BlazingJob : ComponentBase
{
    [Inject]
    private ISchedulerDefinitionService SchedulerDefSvc { get; set; } = null!;

    [Inject]
    private ISchedulerService SchedulerSvc { get; set; } = null!;

    [Inject]
    private IDialogService DialogSvc { get; set; } = null!;

    [Inject]
    private ILogger<BlazingJob> Logger { get; set; } = null!;

    [Inject]
    private IJobUIProvider JobUIProvider { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public JobDetailModel JobDetail { get; set; } = new();

    [Parameter]
    public bool IsReadOnly { get; set; } = false;

    [Parameter]
    public bool IsValid { get; set; }

    [Parameter]
    public EventCallback<bool> IsValidChanged { get; set; }

    private Key OriginalJobKey = new(string.Empty, "No Group");

    private IEnumerable<Type> AvailableJobTypes = Enumerable.Empty<Type>();
    private IEnumerable<string>? ExistingJobGroups;
    private MudForm _form = null!;
    private Type? JobUIType = null;
    private Dictionary<string, object> JobUITypeParameters = new();
    private DynamicComponent? _jobUIComponent;

    protected override async Task OnInitializedAsync()
    {
        var types = SchedulerDefSvc.GetJobTypes();
        var typeList = new HashSet<Type>(types);
        if (JobDetail.JobClass != null)
        {
            typeList.Add(JobDetail.JobClass);
            await OnJobClassValueChanged(JobDetail.JobClass);
        }
        AvailableJobTypes = typeList;

        OriginalJobKey = new(JobDetail.Name, JobDetail.Group);
    }

    async Task<IEnumerable<string>> SearchJobGroup(string value)
    {
        if (ExistingJobGroups == null)
        {
            ExistingJobGroups = await SchedulerSvc.GetJobGroups();
        }

        if (string.IsNullOrEmpty(value))
            return ExistingJobGroups;

        var matches = ExistingJobGroups
            .Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        if (!matches.Any(x => x == value))
            matches.Add(value);

        return matches;
    }

    private void OnSetIsValid(bool value)
    {
        if (IsValid == value)
            return;
        IsValid = value;
        IsValidChanged.InvokeAsync(value);
    }

    public async Task Validate()
    {
        var jobUI = _jobUIComponent?.Instance as IJobUI;

        if (jobUI != null)
        {
            if (!await jobUI.ApplyChanges())
            {
                OnSetIsValid(false);
                return;
            }
        }

        await _form.Validate();
    }

    private async Task<string?> ValidateJobName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Job name is required";

        // accept if same as original
        if (OriginalJobKey.Equals(name, JobDetail.Group))
            return null;

        if (IsReadOnly)
        {
            Logger.LogDebug("Skip checking of job name uniqueness if in readonly mode");
            return null;
        }

        var detail = await SchedulerSvc.GetJobDetail(name, JobDetail.Group);

        if (detail != null)
            return "Job name already in used. Please choose another name or group.";

        return null;
    }

    private async Task OnJobClassValueChanged(Type jobType)
    {
        JobDetail.JobClass = jobType;

        // clear previous changes
        var jobUI = _jobUIComponent?.Instance as IJobUI;
        if (jobUI != null)
            await jobUI.ClearChanges();

        var jobUIType = JobUIProvider.GetJobUIType(jobType.FullName);
        JobUITypeParameters.Clear();
        JobUITypeParameters[nameof(IsReadOnly)] = IsReadOnly;
        if (jobUIType == typeof(DefaultJobUI))
            JobUITypeParameters[nameof(JobDetail)] = JobDetail;
        else
            JobUITypeParameters[nameof(JobDetail.JobDataMap)] = JobDetail.JobDataMap;
        JobUIType = jobUIType;
    }
}
