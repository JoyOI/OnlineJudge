using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class ProtectedProblemIdPrefix
    {
        [MaxLength(32)]
        public string Value { get; set; }
    }
}
