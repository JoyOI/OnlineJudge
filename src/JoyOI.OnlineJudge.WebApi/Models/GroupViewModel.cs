using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class GroupViewModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int MemberCount { get; set; }

        public IEnumerable<string> Masters { get; set; }

        public string LogoUrl { get; set; }

        public string Domain { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public GroupJoinMethod JoinMethod { get; set; }
    }
}
