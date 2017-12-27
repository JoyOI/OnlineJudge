using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class GroupProblem
    {
        [MaxLength(128)]
        [ForeignKey("Group")]
        public string GroupId { get; set; }

        public virtual Group Group { get; set; }

        [MaxLength(128)]
        [ForeignKey("Problem")]
        public string ProblemId { get; set; }

        public virtual Problem Problem { get; set; }

        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }
}
