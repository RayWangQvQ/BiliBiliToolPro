namespace Ray.BiliBiliTool.Web.Services;

public interface IJobUIProvider
{
    Type GetJobUIType(string? jobTypeFullName);
}
