using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    /// <summary>
    /// Protected problem identifier prefix.
    /// </summary>
    public class ProtectedProblemIdPrefix
    {
        /// <summary>
        /// Gets or sets the protected prefix value.
        /// </summary>
        /// <value>The value.</value>
        [MaxLength(32)]
        public string Value { get; set; }
    }
}
