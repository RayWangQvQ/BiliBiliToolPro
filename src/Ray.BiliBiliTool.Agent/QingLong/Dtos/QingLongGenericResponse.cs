namespace Ray.BiliBiliTool.Agent.QingLong.Dtos;

public class QingLongGenericResponse<T>
{
    public int Code { get; set; }

    public required T Data { get; set; }
}
