using System;
using System.ComponentModel.DataAnnotations;

namespace JoyOI.OnlineJudge.Models
{
    public class StateMachine
    {
        public Guid Id { get; set; }

        public DateTime CreatedTime { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }
    }
}
