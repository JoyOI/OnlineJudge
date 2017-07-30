using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Judge status.
    /// </summary>
    public class JudgeStatus
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        public JudgeResult Result { get; set; }

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

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        [MaxLength(16)]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        /// <value>The time.</value>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the compiled code BLOB identifier.
        /// </summary>
        /// <value>The binary BLOB identifier.</value>
        public Guid? BinaryBlobId { get; set; }

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
        /// Gets or sets the related state machine identifiers.
        /// </summary>
        /// <value>The related state machine identifiers.</value>
        public virtual ICollection<StateMachine> RelatedStateMachineIds { get; set; }

        /// <summary>
        /// Gets or sets the sub statuses.
        /// </summary>
        /// <value>The sub statuses.</value>
        public virtual ICollection<SubJudgeStatus> SubStatuses { get; set; }
    }
}
