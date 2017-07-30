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
        /// Gets or sets a value indicating whether this <see cref="T:JoyOI.OnlineJudge.Models.VirtualJudgeUser"/> is in use.
        /// </summary>
        /// <value><c>true</c> if is in use; otherwise, <c>false</c>.</value>
        public bool IsInUse { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source of this user.</value>
        public ProblemSource Source { get; set; }
    }
}
