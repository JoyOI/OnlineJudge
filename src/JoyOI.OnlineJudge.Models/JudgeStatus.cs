using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        [JsonConverter(typeof(StringEnumConverter))]
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
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        [WebApi(FilterLevel.GetNeedOwner | FilterLevel.GetListDisabled)]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the compiled code BLOB identifier.
        /// </summary>
        /// <value>The binary BLOB identifier.</value>
        [WebApi(FilterLevel.GetListDisabled | FilterLevel.GetSingleDisabled)]
        public Guid? BinaryBlobId { get; set; }

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
        /// Gets or sets the hint
        /// </summary>
        /// <value>The hint of this status</value>
        [WebApi(FilterLevel.GetListDisabled)]
        public string Hint { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the status is self test.
        /// </summary>
        /// <value>The status is self test or not</value>
        public bool IsSelfTest { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the status is hackable.
        /// </summary>
        [NotMapped]
        public bool? IsHackable { get; set; }

        /// <summary>
        /// Gets or sets the related state machine identifiers.
        /// </summary>
        /// <value>The related state machine identifiers.</value>
        public virtual ICollection<JudgeStatusStateMachine> RelatedStateMachineIds { get; set; }

        /// <summary>
        /// Gets or sets the sub statuses.
        /// </summary>
        /// <value>The sub statuses.</value>
        [ForceInclude]
        public virtual ICollection<SubJudgeStatus> SubStatuses { get; set; }
    }
}
