using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public enum ClarificationStatus
    {
        Pending,
        Responsed,
        Broadcasted
    }

    public class Clarification
    {
        public Guid Id { get; set; }

        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        public virtual Contest Contest { get; set; }

        [ForeignKey("Problem")]
        public string ProblemId { get; set; }

        public virtual Problem Problem { get; set; }

        [ForeignKey("RequestUser")]
        public Guid RequestUserId { get; set; }

        public virtual User RequestUser { get; set; }

        [ForeignKey("ResponseUser")]
        public Guid? ResponseUserId { get; set; }

        public virtual User ResponseUser { get; set; }

        public string RequestText { get; set; }

        public string ResponseText { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.Now;

        public ClarificationStatus Status { get; set; }
    }
}
