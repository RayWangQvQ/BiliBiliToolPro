namespace Ray.BiliBiliTool.Agent.Baihu.Dtos;

public class BaihuGenericResponse<T>
{
    public int Code { get; set; }

    public string Msg { get; set; }

    public T Data { get; set; }
}
