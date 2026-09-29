namespace Ray.BiliBiliTool.Web.Services;

/// <summary>
/// Read model for the single panel administrator account behind /Admin.
/// </summary>
public sealed record AdminAccountInfo(string Username, IReadOnlyList<string> Roles);
