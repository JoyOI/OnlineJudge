using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class SharedValidator
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(16)]
        public string Language { get; set; }

        public string Code { get; set; }

        public bool IsDefault { get; set; }
    }
}
