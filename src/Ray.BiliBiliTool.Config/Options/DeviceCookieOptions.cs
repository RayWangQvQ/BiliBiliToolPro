namespace Ray.BiliBiliTool.Config.Options;

/// <summary>
/// Pinned device fingerprint Cookie configuration.
/// </summary>
/// <remarks>
/// The -403 "account abnormal" risk control on Bilibili's share endpoint (share/add) is bound to the
/// device profile behind buvid3/buvid4/b_nut: every call to /x/frontend/finger/spi returns a brand-new
/// device pair with no browsing history, which is then rejected as "account abnormal"
/// (verified by an A/B experiment on the production container, 2026-09-15).
/// Configure a device pair copied from a real browser (long-lived, with normal browsing history);
/// the program then pins those values and stops calling finger/spi to refresh them.
/// </remarks>
public class DeviceCookieOptions
{
    public const string SectionName = "DeviceCookie";

    /// <summary>
    /// Pinned buvid3 value (copied from your browser Cookie)
    /// </summary>
    public string? Buvid3 { get; set; }

    /// <summary>
    /// Pinned buvid4 value (copied from your browser Cookie)
    /// </summary>
    public string? Buvid4 { get; set; }

    /// <summary>
    /// Pinned b_nut value (copied from your browser Cookie)
    /// </summary>
    public string? BNut { get; set; }

    /// <summary>
    /// Whether any pinned device Cookie value is configured
    /// </summary>
    public bool HasPinnedValues =>
        !string.IsNullOrWhiteSpace(Buvid3)
        || !string.IsNullOrWhiteSpace(Buvid4)
        || !string.IsNullOrWhiteSpace(BNut);
}
