using System;
using System.Collections.Generic;
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
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime RegisteryTime { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
		[MaxLength(64)]
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled | FilterLevel.GetListDisabled | FilterLevel.GetSingleDisabled)]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the access token expire time.
        /// </summary>
        /// <value>The expire time.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled | FilterLevel.GetListDisabled | FilterLevel.GetSingleDisabled)]
        public DateTime ExpireTime { get; set; }

        /// <summary>
        /// Gets or sets the open identifier.
        /// </summary>
        /// <value>The open identifier.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public Guid OpenId { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        /// <value>The avatar URL.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public string AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the active time.
        /// </summary>
        /// <value>The active time.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime ActiveTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last login time.
        /// </summary>
        /// <value>The last login time.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime LastLoginTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the passed problems.
        /// </summary>
        /// <value>The passed problems.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public JsonObject<List<string>> PassedProblems { get; set; }

        /// <summary>
        /// Gets or sets the tried problems.
        /// </summary>
        /// <value>The tried problems.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public JsonObject<List<string>> TriedProblems { get; set; }
    }
}
