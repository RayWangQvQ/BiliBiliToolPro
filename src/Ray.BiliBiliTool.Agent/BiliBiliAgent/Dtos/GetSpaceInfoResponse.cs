namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class GetSpaceInfoResponse
    {
        public int Mid { get; set; }

        public string Name { get; set; }

        public SpaceLiveRoomInfoDto Live_room { get; set; }
    }

    public class SpaceLiveRoomInfoDto
    {
        public string Title { get; set; }

        public int Roomid { get; set; }
    }
}
