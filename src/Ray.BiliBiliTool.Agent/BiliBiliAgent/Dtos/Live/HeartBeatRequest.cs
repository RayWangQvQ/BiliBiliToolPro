﻿using Ray.BiliBiliTool.Agent.BiliBiliAgent.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos.Live
{
    public class HeartBeatRequest
    {
        public HeartBeatRequest(
            int roomId,
            int parentId,
            int areaID,
            int seqNumber, // 心跳包编号
            string buvid, // cookie['LIVE_BUVID']
            long timestamp,
            long ets, // 由后端返回的 timestamp 
            string userAgent,
            ICollection<int> secretRule,
            string secretKey,
            string csrf,
            string uuid)
        {
            this.Id = JsonSerializer.Serialize(new[] { parentId, areaID, seqNumber, roomId });
            // this.Device = JsonSerializer.Serialize(new[] { buvid, uuid });
            this.Ets = ets;
            this.Benchmark = secretKey;
            this.Time = 60;
            this.Ts = timestamp;
            this.Ua = userAgent;
            this.Csrf = csrf;

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

            Console.WriteLine(secretKey);
            Console.WriteLine(JsonSerializer.Serialize(secretRule));
            this.S = LiveHeartBeatCrypto.Sypder(jsonString, secretRule, secretKey);

            this.Visit_id = "";
        }

        public string S { get; set; }

        public string Id { get; set; }

        // public string Device { get;set; }

        public long Ets { get; set; }

        public string Benchmark { get; set; }

        public long Time { get; set; }

        public long Ts { get; set; }

        public string Ua { get; set; }

        public string Csrf_token => Csrf;

        public string Csrf { get; set; }

        public string Visit_id { get; set; }

    }
}
