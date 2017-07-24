using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class HackStatus
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        [ForeignKey("Status")]
        public Guid JudgeStatusId { get; set; }

        public JudgeStatus Status { get; set; }

        public DateTime Time { get; set; }

        public string HackData { get; set; }

        public HackResult Result { get; set; }
    }
}
