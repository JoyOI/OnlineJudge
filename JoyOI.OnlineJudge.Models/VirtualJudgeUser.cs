using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class VirtualJudgeUser
    {
        public Guid Id { get; set; }

        [MaxLength(32)]

        public string Username { get; set; }

        [MaxLength(64)]
        public string Password { get; set; }

        public bool IsInUse { get; set; }

        public ProblemSource Source { get; set; }
    }
}
