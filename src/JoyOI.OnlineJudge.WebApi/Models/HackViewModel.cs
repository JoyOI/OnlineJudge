using System;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class HackViewModel
    {
        public Guid Id { get; set; }

        public Guid HackerId { get; set; }

        public Guid HackeeId { get; set; }

        public HackResult HackResult { get; set; }

        public JudgeResult JudgeResult { get; set; }

        public int TimeUsedInMs { get; set; }

        public int MemoryUsedInByte { get; set; }

        public Guid JudgeStatusId { get; set; }

        public DateTime Time { get; set; }
    }
}
