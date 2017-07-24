using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class JudgeStatus
    {
        public Guid Id { get; set; }
        
        public JudgeResult Result { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        [MaxLength(128)]
        [ForeignKey("Problem")]
        public string ProblemId { get; set; }

        public virtual Problem Problem { get; set; }

        [MaxLength(8)]
        public string Language { get; set; }

        public DateTime Time { get; set; }

        public string Code { get; set; }

        public Guid? BinaryBlobId { get; set; }

        [MaxLength(128)]
        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        public virtual Contest Contest { get; set; }

        public virtual ICollection<StateMachine> RelatedStateMachineIds { get; set; }
    }
}
