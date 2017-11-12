using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// State machine.
    /// </summary>
    public class StateMachine
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the created time.
        /// </summary>
        /// <value>The created time.</value>
        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [MaxLength(64)]
        public string Name { get; set; }
    }
}
