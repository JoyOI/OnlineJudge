using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Group join request status.
    /// </summary>
    public enum GroupMemberStatus
    {
        Pending,
        Approved,
        Rejected
    }

    /// <summary>
    /// Group join request.
    /// </summary>
    public class GroupMember
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        [MaxLength(128)]
        [ForeignKey("Group")]
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets the group.
        /// </summary>
        /// <value>The group.</value>
        public virtual Group Group { get; set; }

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
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        /// <value>The created time.</value>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public GroupMemberStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the feedback.
        /// </summary>
        /// <value>The feedback.</value>
        public string Feedback { get; set; }
    }
}
