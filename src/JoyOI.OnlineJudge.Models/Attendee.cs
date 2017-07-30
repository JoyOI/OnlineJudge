using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class Attendee
    {
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
        /// Gets or sets the contest identifier.
        /// </summary>
        /// <value>The contest identifier.</value>
        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        /// <summary>
        /// Gets or sets the contest.
        /// </summary>
        /// <value>The contest.</value>
        public virtual Contest Contest { get; set; }

        /// <summary>
        /// Gets or sets the register time.
        /// </summary>
        /// <value>The register time.</value>
        public DateTime RegisterTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user registered a virtual contest.
        /// </summary>
        /// <value><c>true</c> if is virtual; otherwise, <c>false</c>.</value>
        public bool IsVirtual { get; set; }
    }
}
