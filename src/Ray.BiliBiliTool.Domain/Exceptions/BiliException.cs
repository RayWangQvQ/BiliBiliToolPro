namespace Ray.BiliBiliTool.Domain.Exceptions;

public abstract class BiliException : Exception
{
    protected BiliException(string message)
        : base(message) { }

    protected BiliException(string message, Exception innerException)
        : base(message, innerException) { }
}
