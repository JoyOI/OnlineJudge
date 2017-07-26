using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Contest problem last status.
    /// </summary>
    public class ContestProblemLastStatus
    {
        #region Keys
        /// <summary>
        /// Gets or sets the contest identifier.
        /// </summary>
        /// <value>The contest identifier.</value>
        [MaxLength(128)]
        [ForeignKey("Contest User")]
        public string ContestId { get; set; }

        /// <summary>
        /// Gets or sets the contest.
        /// </summary>
        /// <value>The contest.</value>
        public virtual Contest Contest { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public virtual User User { get; set; }

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
        #endregion

        /// <summary>
        /// Gets or sets the status identifier.
        /// </summary>
        /// <value>The status identifier.</value>
        [ForeignKey("Status")]
        public Guid StatusId { get; set; }

        /// <summary>
        /// Gets or sets the judge status.
        /// </summary>
        /// <value>The status.</value>
        public virtual JudgeStatus Status { get; set; }

        #region Standings fields
        /// <summary>
        /// Gets or sets the point #1.
        /// </summary>
        /// <value>The point #1.</value>
        public int Point { get; set; }

        /// <summary>
        /// Gets or sets the point #2.
        /// </summary>
        /// <value>The point2.</value>
        public int Point #2 { get; set; }

        /// <summary>
        /// Gets or sets the point #3.
        /// </summary>
        /// <value>The point #3.</value>
        public int Point3 { get; set; }

        /// <summary>
        /// Gets or sets the time span.
        /// </summary>
        /// <value>The time span #1.</value>
        public TimeSpan TimeSpan { get; set; }

        /// <summary>
        /// Gets or sets the time span #2.
        /// </summary>
        /// <value>The time span #2.</value>
        public TimeSpan TimeSpan2 { get; set; }
        #endregion
    }
}
