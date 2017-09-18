using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class Configuration
    {
        [Key]
        [MaxLength(32)]
        public string Key { get; set; }

        public string Value { get; set; }
    }
}
