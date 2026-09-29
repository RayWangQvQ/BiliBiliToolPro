using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Services.Pages.Admin;

public class AdminPageWorkflow(IAuthService authService) : IAdminPageWorkflow
{
    public async Task<AdminAccountChangeResult> ChangePasswordAsync(
        AdminPasswordChangeRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return new AdminAccountChangeResult(false, "新密码不能为空", null);

        if (request.NewPassword != request.ConfirmPassword)
            return new AdminAccountChangeResult(false, "两次输入的新密码不一致", null);

        return await ExecuteAsync(
            () => authService.ChangePasswordAsync(request.CurrentPassword, request.NewPassword),
            "密码修改成功"
        );
    }

    public async Task<AdminAccountChangeResult> ChangeUsernameAsync(
        AdminUsernameChangeRequest request
    )
    {
        var newUsername = request.NewUsername.Trim();

        if (string.IsNullOrWhiteSpace(newUsername))
            return new AdminAccountChangeResult(false, "新用户名不能为空", null);

        return await ExecuteAsync(
            () => authService.ChangeUsernameAsync(newUsername, request.CurrentPassword),
            "用户名已更新"
        );
    }

    private static async Task<AdminAccountChangeResult> ExecuteAsync(
        Func<Task> operation,
        string successMessage
    )
    {
        try
        {
            await operation();
            return new AdminAccountChangeResult(true, null, successMessage);
        }
        catch (Exception e)
        {
            return new AdminAccountChangeResult(false, e.Message, null);
        }
    }
}
