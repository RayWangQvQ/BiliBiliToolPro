namespace Ray.BiliBiliTool.Web.Services.Pages.Admin;

/// <summary>
/// Web-layer contract for the Admin page account mutations. Changing the login
/// name and changing the password are separate operations (ADR-0009).
/// </summary>
public interface IAdminPageWorkflow
{
    Task<AdminAccountChangeResult> ChangePasswordAsync(AdminPasswordChangeRequest request);

    Task<AdminAccountChangeResult> ChangeUsernameAsync(AdminUsernameChangeRequest request);
}
