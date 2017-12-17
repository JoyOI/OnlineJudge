using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Hack status.
    /// </summary>
    public class HackStatus
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

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
        /// Gets or sets the judge status identifier.
        /// </summary>
        /// <value>The judge status identifier.</value>
        [ForeignKey("Status")]
        public Guid JudgeStatusId { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public virtual JudgeStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        /// <value>The time.</value>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the hack data BLOB identifier.
        /// </summary>
        /// <value>The hack data BLOB identifier.</value>
        public Guid? HackDataBlobId { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        public HackResult Result { get; set; }

        /// <summary>
        /// Gets or set the hackee result
        /// </summary>
        /// <value>The hackee's result</value>
        public JudgeResult HackeeResult { get; set; }

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
        /// Gets or sets the hint.
        /// </summary>
        /// <value>The hint.</value>
        public string Hint { get; set; }

        /// <summary>
        /// Gets or sets the memory used (byte)
        /// </summary>
        /// <value>The memory used in byte</value>
        public int MemoryUsedInByte { get; set; }

        /// <summary>
        /// Gets or sets the time used (ms)
        /// </summary>
        /// <value>The time used in ms</value>
        public int TimeUsedInMs { get; set; }

        public virtual ICollection<HackStatusStateMachine> RelatedStateMachineIds { get; set; }
    }
}
