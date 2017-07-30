using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// User.
    /// </summary>
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// Gets or sets the registery time.
        /// </summary>
        /// <value>The registery time.</value>
        [Readonly]
        public DateTime RegisteryTime { get; set; }

        /// <summary>
        /// Gets or sets the nickname.
        /// </summary>
        /// <value>The nickname.</value>
		[MaxLength(64)]
		public string Nickname { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
		[MaxLength(64)]
		public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the access token expire time.
        /// </summary>
        /// <value>The expire time.</value>
		public DateTime ExpireTime { get; set; }

        /// <summary>
        /// Gets or sets the open identifier.
        /// </summary>
        /// <value>The open identifier.</value>
		public Guid OpenId { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        /// <value>The avatar URL.</value>
		[MaxLength(256)]
		public string AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the active time.
        /// </summary>
        /// <value>The active time.</value>
        [Readonly]
        public DateTime ActiveTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last login time.
        /// </summary>
        /// <value>The last login time.</value>
        [Readonly]
        public DateTime LastLoginTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the passed problems.
        /// </summary>
        /// <value>The passed problems.</value>
        public JsonObject<string> PassedProblems { get; set; }

        /// <summary>
        /// Gets or sets the tried problems.
        /// </summary>
        /// <value>The tried problems.</value>
        public JsonObject<string> TriedProblems { get; set; }
    }
}
