using Ray.BiliBiliTool.Web.Services;

namespace Ray.BiliBiliTool.Web.Services.Pages.Admin;

public class AdminPageWorkflow(IAuthService authService) : IAdminPageWorkflow
{
    public async Task<AdminPasswordChangeResult> ChangePasswordAsync(
        AdminPasswordChangeRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return new AdminPasswordChangeResult(false, "Password cannot be empty", null);

        if (request.NewPassword != request.ConfirmPassword)
            return new AdminPasswordChangeResult(
                false,
                "The new password and the confirm password do not match",
                null
            );

        try
        {
            await authService.ChangePasswordAsync(
                request.Username,
                request.CurrentPassword,
                request.NewPassword
            );
            return new AdminPasswordChangeResult(true, null, "Password updated successfully.");
        }
        catch (Exception e)
        {
            return new AdminPasswordChangeResult(false, e.Message, null);
        }
    }
}
