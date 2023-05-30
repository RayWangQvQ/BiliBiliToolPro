namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

public class GetSpaceInfoDto
{
    public long mid { get; set; }
}
public class GetSpaceInfoFullDto : GetSpaceInfoDto
{
    public string w_rid { get; set; } 

    public long wts { get; set; } 
}
