using System.Collections.Generic;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class JudgeRequest
    {
        public string code { get; set; }

        public string language { get; set; }

        public string problemId { get; set; }

        public string contestId { get; set; }

        public bool isSelfTest { get; set; }

        public IEnumerable<JudgeRequestData> data { get; set; }
    }

    public class JudgeRequestData
    {
        public string input { get; set; }

        public string output { get; set; }
    }
}
