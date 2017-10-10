using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Sub judge status.
    /// </summary>
    public class SubJudgeStatus
    {
        /// <summary>
        /// Gets or sets the status identifier.
        /// </summary>
        /// <value>The status identifier.</value>
        [ForeignKey("Status")]
        public Guid StatusId { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        [JsonIgnore]
        public virtual JudgeStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the sub id
        /// </summary>
        /// <value>The sub id</value>
        public int SubId { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        [MaxLength(32)]
        public JudgeResult Result { get; set; }

        /// <summary>
        /// Gets or sets the test case identifier.
        /// </summary>
        /// <value>The test case identifier.</value>
        [ForeignKey("TestCase")]
        public Guid? TestCaseId { get; set; }

        /// <summary>
        /// Gets or sets the test case.
        /// </summary>
        /// <value>The test case.</value>
        public virtual TestCase TestCase { get; set; }

        /// <summary>
        /// Gets or sets the input blob id
        /// </summary>
        public Guid InputBlobId { get; set; }

        /// <summary>
        /// Gets or sets the output blob id in management service
        /// </summary>
        /// <value>The output blob id</value>
        public Guid OutputBlobId { get; set; }

        /// <summary>
        /// Gets or sets the hint
        /// </summary>
        /// <value>The hint</value>
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
    }
}
