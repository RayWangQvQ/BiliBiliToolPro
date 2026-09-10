namespace Ray.BiliBiliTool.Domain.Exceptions;

/// <summary>API returned a non-success business code — expected, recoverable.</summary>
public class BiliBusinessException : BiliException
{
    public BiliBusinessException(string message)
        : base(message) { }

    public BiliBusinessException(string message, Exception innerException)
        : base(message, innerException) { }
}
