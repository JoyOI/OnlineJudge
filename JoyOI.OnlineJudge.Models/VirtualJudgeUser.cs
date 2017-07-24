using System;

namespace JoyOI.OnlineJudge.Models
{
    public class VirtualJudgeUser
    {
        public Guid Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool IsInUse { get; set; }

        public ProblemSource Source { get; set; }
    }
}
