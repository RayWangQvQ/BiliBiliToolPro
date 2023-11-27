using System.Collections.Generic;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.VipTask;

public class VouchersInfoResponse
{
    public List<List> List { get; set; }
    public bool IsShortVip { get; set; }
    public bool IsFreightOpen { get; set; }
    public int Level { get; set; }
    public int CurExp { get; set; }
    public int NextExp { get; set; }
    public bool IsVip { get; set; }
    public int IsSeniorMember { get; set; }
    public int Format060102 { get; set; }
}


public class List
{
    public int Type { get; set; }
    public int State { get; set; }
    public int ExpireTime { get; set; }
    public int VipType { get; set; }
    public int NextReceiveDays { get; set; }
    public int PeriodEndUnix { get; set; }
}

