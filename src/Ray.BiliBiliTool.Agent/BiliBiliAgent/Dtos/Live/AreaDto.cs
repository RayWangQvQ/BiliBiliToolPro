using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live;

public class GetArteaListResponse
{
    public List<AreaDto> Data { get; set; }
}

public class AreaDto
{
    public long Id { get; set; }

    public string Name { get; set; }
}
