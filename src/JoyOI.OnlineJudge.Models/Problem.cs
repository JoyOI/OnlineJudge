using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Problem.
    /// </summary>
    public class Problem
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [MaxLength(128)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [MaxLength(128)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body(markdown formatted).
        /// </summary>
        /// <value>The body.</value>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the tags, e.g. "DP:Linear", "Others:Force".
        /// </summary>
        /// <value>The tags.</value>
        [MaxLength(1024)]
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets the validator code.
        /// </summary>
        /// <value>The validator code.</value>
        public string ValidatorCode { get; set; }

		/// <summary>
		/// Gets or sets the validator language.
		/// </summary>
		/// <value>The validator language.</value>
		[MaxLength(16)]
        public string ValidatorLanguage { get; set; }

        /// <summary>
        /// Gets or sets the validator BLOB identifier.
        /// </summary>
        /// <value>The validator BLOB identifier.</value>
        public Guid? ValidatorBlobId { get; set; }

        /// <summary>
        /// Gets or sets the validator error
        /// </summary>
        /// <value>The validator error</value>
        public string ValidatorError { get; set; }

        /// <summary>
        /// Gets or sets the standard code.
        /// </summary>
        /// <value>The standard code.</value>
        public string StandardCode { get; set; }

		/// <summary>
		/// Gets or sets the standard language.
		/// </summary>
		/// <value>The standard language.</value>
		[MaxLength(16)]
        public string StandardLanguage { get; set; }

        /// <summary>
        /// Gets or sets the standard program error
        /// </summary>
        /// <value>The standard program error</value>
        public string StandardError { get; set; }

        /// <summary>
        /// Gets or sets the standard BLOB identifier.
        /// </summary>
        /// <value>The standard BLOB identifier.</value>
        public Guid? StandardBlobId { get; set; }

        /// <summary>
        /// Gets or sets the range code.
        /// </summary>
        /// <value>The range code.</value>
        public string RangeCode { get; set; }

        /// <summary>
        /// Gets or sets the range language.
        /// </summary>
        /// <value>The range language.</value>
        [MaxLength(16)]
        public string RangeLanguage { get; set; }

        /// <summary>
        /// Gets or sets the range BLOB identifier.
        /// </summary>
        /// <value>The range BLOB identifier.</value>
        public Guid? RangeBlobId { get; set; }

        /// <summary>
        /// Gets or sets the range validator error
        /// </summary>
        /// <value>The range validator error</value>
        public string RangeError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:JoyOI.OnlineJudge.Models.Problem"/> is visiable.
        /// </summary>
        /// <value><c>true</c> if is visiable; otherwise, <c>false</c>.</value>
        public bool IsVisiable { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        public ProblemSource Source { get; set; }

        /// <summary>
        /// Gets or sets the difficulty.
        /// </summary>
        /// <value>The difficulty.</value>
        public int Difficulty { get; set; }

        /// <summary>
        /// Gets or sets the cached submit count.
        /// </summary>
        /// <value>The cached submit count.</value>
        [Readonly]
        public int CachedSubmitCount { get; set; }

        /// <summary>
        /// Gets or sets the cached accepted count.
        /// </summary>
        /// <value>The cached accepted count.</value>
        [Readonly]
        public int CachedAcceptedCount { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        /// <value>The created time</value>
        [Readonly]
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
