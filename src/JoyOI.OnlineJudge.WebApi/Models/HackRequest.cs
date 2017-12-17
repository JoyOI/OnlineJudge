using System;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class HackRequest
    {
        public Guid JudgeStatusId { get; set; }

        public string Data { get; set; }

        public bool IsBase64 { get; set; }

        public string ContestId { get; set; }
    }
}
