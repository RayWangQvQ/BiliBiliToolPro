namespace Ray.BiliBiliTool.Web.Services.Pages.Admin;

/// <summary>
/// Web-layer contract for the Admin page password-change workflow.
/// Phase 14 provides the concrete implementation; this phase defines
/// the contract and result shape so Phase 14 can migrate the component
/// without reopening API design questions.
/// </summary>
public interface IAdminPageWorkflow
{
    Task<AdminPasswordChangeResult> ChangePasswordAsync(AdminPasswordChangeRequest request);
}
