using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class ContestProblem
    {
        [MaxLength(128)]
        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        public virtual Contest Contest { get; set; }

        [MaxLength(128)]
        [ForeignKey("Problem")]
        public string ProblemId { get; set; }

        public virtual Problem Problem { get; set; }

        public int Point { get; set; }

        [MaxLength(16)]
        public string Number { get; set; }
    }
}
