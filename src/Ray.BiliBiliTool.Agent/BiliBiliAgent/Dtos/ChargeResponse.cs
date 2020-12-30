namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos
{
    public class ChargeResponse
    {
        public int Status { get; set; }

        public string Order_no { get; set; }
    }

    public class ChargeV2Response
    {
        public string Bp_num { get; set; }

        public decimal Exp { get; set; }

        public long Mid { get; set; }

        public string Msg { get; set; }

        public string Order_no { get; set; }

        public int Status { get; set; }

        public int Up_mid { get; set; }
    }
}
