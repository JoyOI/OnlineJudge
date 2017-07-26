using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Test case.
    /// </summary>
    public class TestCase
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the input BLOB identifier.
        /// </summary>
        /// <value>The input BLOB identifier.</value>
        public Guid InputBlobId { get; set; } // Management Service Blob ID

        /// <summary>
        /// Gets or sets the output BLOB identifier.
        /// </summary>
        /// <value>The output BLOB identifier.</value>
        public Guid OutputBlobId { get; set; }

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
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public TestCaseType Type { get; set; }

        /// <summary>
        /// Gets or sets the contest identifier(nullable).
        /// </summary>
        /// <value>The contest identifier.</value>
        public string ContestId { get; set; }

        /// <summary>
        /// Gets or sets the contest.
        /// </summary>
        /// <value>The contest.</value>
        public virtual Contest Contest { get; set; }
    }
}
