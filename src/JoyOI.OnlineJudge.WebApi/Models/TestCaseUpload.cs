using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class TestCaseUpload
    {
        public string Input { get; set; }

        public string Output { get; set; }

        public TestCaseType Type { get; set; }

        public string ContestId { get; set; }
    }
}
