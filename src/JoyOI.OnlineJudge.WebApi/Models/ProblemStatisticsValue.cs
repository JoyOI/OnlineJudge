namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class ProblemStatisticsValue
    {
        public int NonAccepted { get; set; }

        public int Accepted { get; set; }

        public int Total => NonAccepted + Accepted;
    }
}
