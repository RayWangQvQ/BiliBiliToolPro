using System;

namespace Ray.BiliBiliTool.Agent.QingLong.Dtos;

public class QingLongEnv : UpdateQingLongEnv
{
    public string timestamp { get; set; }
    public int status { get; set; }

    //public long position { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}
