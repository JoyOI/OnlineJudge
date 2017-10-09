namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class Runner
    {
        public int UserTime { get; set; }
        public int TotalTime { get; set; }
        public int ExitCode { get; set; }
        public int PeakMemory { get; set; }
        public bool IsTimeout { get; set; }
        public string Error { get; set; }
    }
}
