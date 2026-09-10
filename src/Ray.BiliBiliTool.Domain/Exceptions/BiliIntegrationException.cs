namespace Ray.BiliBiliTool.Domain.Exceptions;

/// <summary>Network or external system failure — HTTP errors, timeout, QingLong down.</summary>
public class BiliIntegrationException : BiliException
{
    public BiliIntegrationException(string message)
        : base(message) { }

    public BiliIntegrationException(string message, Exception innerException)
        : base(message, innerException) { }
}
