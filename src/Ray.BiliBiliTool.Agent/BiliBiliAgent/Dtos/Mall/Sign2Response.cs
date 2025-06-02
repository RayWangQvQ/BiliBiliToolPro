using System.Text;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Mall;

public class Sign2Response
{
    public int count { get; set; }
    public int countdown { get; set; }
    public int duration { get; set; }
    public bool hasCoupon { get; set; }
    public int score { get; set; }
    public int vipScore { get; set; }
    public int vipStatus { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"获得经验：{score}");
        sb.AppendLine($"累计签到：{count}/{duration} 天");

        return sb.ToString();
    }
}
