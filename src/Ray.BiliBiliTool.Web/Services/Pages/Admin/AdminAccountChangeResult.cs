namespace Ray.BiliBiliTool.Web.Services.Pages.Admin;

/// <summary>
/// Input for changing the administrator password. The login name is not part of
/// this request - renaming is a separate operation (ADR-0009).
/// </summary>
public sealed record AdminPasswordChangeRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

/// <summary>
/// Input for changing the administrator login name. The current password
/// authorises the change.
/// </summary>
public sealed record AdminUsernameChangeRequest(string NewUsername, string CurrentPassword);

/// <summary>
/// Outcome shared by both account mutations. Three branches:
/// validation failure (IsSuccess=false, ErrorMessage set),
/// service failure (IsSuccess=false, ErrorMessage set),
/// success (IsSuccess=true, SuccessMessage set).
/// </summary>
public sealed record AdminAccountChangeResult(
    bool IsSuccess,
    string? ErrorMessage,
    string? SuccessMessage
);
