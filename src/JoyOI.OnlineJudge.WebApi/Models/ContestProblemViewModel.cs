namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class ContestProblemViewModel
    {
        public string problemId { get; set; }

        public int point { get; set; }

        public string number { get; set; }

        public string status { get; set; }

        public bool isVisible { get; set; }
    }
}
