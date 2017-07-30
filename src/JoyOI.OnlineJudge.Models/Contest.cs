using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Contest type.
    /// </summary>
    public enum ContestType
    {
        OI,
        ACM,
        Codeforces,
        TopCoder
    }

    /// <summary>
    /// Attend permission.
    /// </summary>
    public enum AttendPermission
    {
        Everyone,
        Team,
        Password
    }

    /// <summary>
    /// Contest Model
    /// </summary>
    public class Contest
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [MaxLength(128)]
		public string Id { get; set; }

        /// <summary>
        /// Gets or sets the domain name.
        /// </summary>
        /// <value>The domain.</value>
		[MaxLength(256)]
		public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [MaxLength(128)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the contest type <see cref="ContestType"/>.
        /// </summary>
        /// <value>The type.</value>
        public ContestType Type { get; set; }

        /// <summary>
        /// Gets or sets the begin time.
        /// </summary>
        /// <value>The begin.</value>
        public DateTime Begin { get; set; }

        /// <summary>
        /// Gets or sets the contest duration.
        /// </summary>
        /// <value>The duration.</value>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the contest end time.
        /// </summary>
        /// <value>The end.</value>
        [NotMapped]
        public DateTime End => Begin.Add(Duration);

        /// <summary>
        /// Gets or sets the attend permission type.
        /// </summary>
        /// <value>The attend permission.</value>
        public AttendPermission AttendPermission { get; set; }

        /// <summary>
        /// Gets or sets the password or team, for password, it should be the password string, for team, it should be the team identifier.
        /// </summary>
        /// <value>The password or team.</value>
        public string PasswordOrTeam { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the virtual mode has been disabled.
        /// </summary>
        /// <value><c>true</c> if disable virtual; otherwise, <c>false</c>.</value>
        public bool DisableVirtual { get; set; }

        /// <summary>
        /// Gets or sets the banned languages.
        /// </summary>
        /// <value>The banned languages.</value>
        public string BannedLanguages { get; set; }

        /// <summary>
        /// Gets or sets the banned languages array.
        /// </summary>
        /// <value>The banned languages array.</value>
        public IEnumerable<string> BannedLanguagesArray
        {
            get => BannedLanguages.Split(',').Select(x => x.Trim()); 
        }
    }
}
