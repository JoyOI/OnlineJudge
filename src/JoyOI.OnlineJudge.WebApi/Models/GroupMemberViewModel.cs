using System;
using JoyOI.OnlineJudge.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class GroupMemberViewModel
    {
        public Guid UserId { get; set; }

        public bool IsMaster { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public GroupMemberStatus Status { get; set; }

        public string Request { get; set; }

        public string Response { get; set; }

        public DateTime JoinedTime { get; set; }
    }
}
