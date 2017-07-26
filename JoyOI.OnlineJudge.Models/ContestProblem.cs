using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Contest problem.
    /// </summary>
    public class ContestProblem
    {
        /// <summary>
        /// Gets or sets the contest identifier.
        /// </summary>
        /// <value>The contest identifier.</value>
        [MaxLength(128)]
        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        /// <summary>
        /// Gets or sets the contest.
        /// </summary>
        /// <value>The contest.</value>
        public virtual Contest Contest { get; set; }

        /// <summary>
        /// Gets or sets the problem identifier.
        /// </summary>
        /// <value>The problem identifier.</value>
        [MaxLength(128)]
        [ForeignKey("Problem")]
        public string ProblemId { get; set; }

        /// <summary>
        /// Gets or sets the problem.
        /// </summary>
        /// <value>The problem.</value>
        public virtual Problem Problem { get; set; }

        /// <summary>
        /// Gets or sets the point of this problem.
        /// </summary>
        /// <value>The point.</value>
        public int Point { get; set; }

        /// <summary>
        /// Gets or sets the contest problem number.
        /// </summary>
        /// <value>The number.</value>
        [MaxLength(16)]
        public string Number { get; set; }
    }
}
