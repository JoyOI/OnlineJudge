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
        /// <value>The point #2.</value>
        public int Point2 { get; set; }

        /// <summary>
        /// Gets or sets the point #3.
        /// </summary>
        /// <value>The point #3.</value>
        public int Point3 { get; set; }

        /// <summary>
        /// Gets or sets the point #4.
        /// </summary>
        /// <value>The point #4.</value>
        public int Point4 { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether this
        /// <see cref="T:JoyOI.OnlineJudge.Models.ContestProblemLastStatus"/> is locked.
        /// </summary>
        /// <value><c>true</c> if is locked; otherwise, <c>false</c>.</value>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is virtual attendee
        /// </summary>
        /// <value><c>true</c> if is virtual; otherwise, <c>false</c>.</value>
        public bool IsVirtual { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is an accepted result
        /// </summary>
        /// <value><c>true</c> if is virtual; otherwise, <c>false</c>.</value>
        public bool IsAccepted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this status has been hacked
        /// </summary>
        /// <value><c>true</c> if is hacked; otherwise, <c>false</c>.</value>
        public bool IsHacked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this status is hackable
        /// </summary>
        /// <value><c>true</c> if is hackable; otherwise, <c>false</c>.</value>
        public bool IsHackable { get; set; }
        #endregion
    }
}
