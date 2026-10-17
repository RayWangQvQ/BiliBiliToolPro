namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.LiveApi;

public class FansMedalPanelResponse
{
    public List<FansMedalPanelItem> List { get; set; } = [];
    public List<FansMedalPanelItem> Special_list { get; set; } = [];
    public FansMedalPageInfo Page_info { get; set; } = new();
}

public class FansMedalPageInfo
{
    public int Total_page { get; set; }
    public bool Has_more { get; set; }
}

public class FansMedalPanelItem
{
    public FansMedalIdentity Medal { get; set; } = new();
    public FansMedalRoom Room_info { get; set; } = new();
    public FansMedalAnchor Anchor_info { get; set; } = new();
}

public class FansMedalAnchor
{
    public string Nick_name { get; set; } = "";
}

public class FansMedalIdentity
{
    public long Medal_id { get; set; }
    public long Target_id { get; set; }
    public int Level { get; set; }
    public string Medal_name { get; set; } = "";
}

public class FansMedalRoom
{
    public long Room_id { get; set; }
    public int Living_status { get; set; }
}

public class ActivatedMedalResponse
{
    public int Level { get; set; }
    public bool Is_lighted { get; set; }
    public bool Reach_free_intimacy_limit { get; set; }
    public List<FansMedalTaskInfo> Task_info { get; set; } = [];
}

public class FansMedalTaskInfo
{
    public string Title { get; set; } = "";
    public string Sub_title { get; set; } = "";
    public string Jump_type { get; set; } = "";
    public bool Is_done { get; set; }
}
