using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Group join method.
    /// </summary>
    public enum GroupJoinMethod
    {
        Everyone,
        Verification
    }

    /// <summary>
    /// Group type.
    /// </summary>
    public enum GroupType
    {
        Private,
        Public
    }

    /// <summary>
    /// Group.
    /// </summary>
    public class Group
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [MaxLength(128)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [MaxLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>The domain.</value>
        [MaxLength(256)]
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the join method.
        /// </summary>
        /// <value>The join method.</value>
        public GroupJoinMethod JoinMethod { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        /// <value>The created time.</value>
        [WebApi(FilterLevel.ReadOnly)]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>The point.</value>
        [WebApi(FilterLevel.ReadOnly)]
        public int Point { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [WebApi(FilterLevel.ReadOnly)]
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the cached member count.
        /// </summary>
        /// <value>The cached member count.</value>
        [WebApi(FilterLevel.ReadOnly)]
        public long CachedMemberCount { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public GroupType Type { get; set; }
    }
}
