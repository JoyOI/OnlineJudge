using System;
using JoyOI.OnlineJudge.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class HackViewModel
    {
        public Guid Id { get; set; }

        public Guid HackerId { get; set; }

        public Guid HackeeId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public HackResult HackResult { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public JudgeResult JudgeResult { get; set; }

        public int TimeUsedInMs { get; set; }

        public int MemoryUsedInByte { get; set; }

        public Guid JudgeStatusId { get; set; }

        public DateTime Time { get; set; }
    }
}
