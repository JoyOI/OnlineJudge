using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Sub judge status.
    /// </summary>
    public class SubJudgeStatus
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

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
        public virtual JudgeStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        [MaxLength(32)]
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the test case identifier.
        /// </summary>
        /// <value>The test case identifier.</value>
        [ForeignKey("TestCase")]
        public Guid TestCaseId { get; set; }

        /// <summary>
        /// Gets or sets the test case.
        /// </summary>
        /// <value>The test case.</value>
        public virtual TestCase TestCase { get; set; }
    }
}
