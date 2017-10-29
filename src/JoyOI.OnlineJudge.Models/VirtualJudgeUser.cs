using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class VirtualJudgeUser
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>The username.</value>
        [MaxLength(32)]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        [MaxLength(64)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the locker id.
        /// </summary>
        /// <value>The locker id.</value>
        public Guid? LockerId { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source of this user.</value>
        public ProblemSource Source { get; set; }
    }
}
