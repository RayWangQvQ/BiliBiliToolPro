using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class HeartBeatRequest
    {
        public HeartBeatRequest(
            long roomId,
            long parentId,
            long areaID,
            int seqNumber, // 心跳包编号
            string buvid, // cookie['LIVE_BUVID']
            long timestamp,
            long ets, // 由后端返回的 timestamp 
            string userAgent,
            ICollection<int> secretRule,
            string secretKey,
            string csrf,
            string uuid,
            string device)
        {
            Id = JsonSerializer.Serialize(new[] { parentId, areaID, seqNumber, roomId });
            Ets = ets;
            Benchmark = secretKey;
            Time = 60;
            Ts = timestamp;
            Ua = userAgent;
            Csrf = csrf;
            Device = device;

            // 构造哈希值
            var json = new
            {
                platform = "web",
                parent_id = parentId,
                area_id = areaID,
                seq_id = seqNumber,
                room_id = roomId,
                buvid,
                uuid,
                ets,
                time = 60,
                ts = timestamp,
            };
            string jsonString = JsonSerializer.Serialize(json);
            this.S = LiveHeartBeatCrypto.Sypder(jsonString, secretRule, secretKey);

            this.Visit_id = "";
        }

        public string S { get; set; }

        public string Id { get; set; }

        public long Ets { get; set; }

        public string Benchmark { get; set; }

        public long Time { get; set; }

        public long Ts { get; set; }

        public string Ua { get; set; }

        public string Csrf_token => Csrf;

        public string Csrf { get; set; }

        public string Visit_id { get; set; }

        public string Device { get; }
    }
}
