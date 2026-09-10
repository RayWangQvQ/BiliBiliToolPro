namespace Ray.BiliBiliTool.Web.Services.Pages.Admin;

/// <summary>
/// Carries the input for the Admin password-change workflow.
/// Models the four fields the Admin page collects from the user.
/// </summary>
public sealed record AdminPasswordChangeRequest(
    string Username,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

/// <summary>
/// Outcome of the Admin password-change workflow.
/// Maps the three result branches that currently exist in Admin.razor.cs:
/// validation failure (IsSuccess=false, ErrorMessage set),
/// service failure (IsSuccess=false, ErrorMessage set),
/// success (IsSuccess=true, SuccessMessage set).
/// </summary>
public sealed record AdminPasswordChangeResult(
    bool IsSuccess,
    string? ErrorMessage,
    string? SuccessMessage
);
