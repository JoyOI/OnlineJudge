using System;
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
        /// Gets or sets the data maker code.
        /// </summary>
        /// <value>The data maker code.</value>
        public string DataMakerCode { get; set; }

        /// <summary>
        /// Gets or sets the data maker language.
        /// </summary>
        /// <value>The data maker language.</value>
        [MaxLength(16)]
        public string DataMakerLanguage { get; set; }

        /// <summary>
        /// Gets or sets the data maker BLOB identifier.
        /// </summary>
        /// <value>The data maker BLOB identifier.</value>
        public Guid? DataMakerBlobId { get; set; }

        /// <summary>
        /// Gets or sets the hack data content.
        /// </summary>
        /// <value>The hack data content.</value>
        public string HackDataContent { get; set; }

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
    }
}
