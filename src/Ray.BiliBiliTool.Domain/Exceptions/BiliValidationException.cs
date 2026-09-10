namespace Ray.BiliBiliTool.Domain.Exceptions;

/// <summary>Input or cookie validation failure — bad cookie format, missing required fields.</summary>
public class BiliValidationException : BiliException
{
    public BiliValidationException(string message)
        : base(message) { }

    public BiliValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
