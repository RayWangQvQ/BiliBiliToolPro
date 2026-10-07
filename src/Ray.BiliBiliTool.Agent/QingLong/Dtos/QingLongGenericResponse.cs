namespace Ray.BiliBiliTool.Agent.QingLong.Dtos;

public class QingLongGenericResponse<T>
{
    public int Code { get; set; }

    public T? Data { get; set; }
}
