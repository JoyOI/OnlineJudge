using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class UserBasic
    {
        public Guid Id { get; set; }

        public string Username { get; set; }

        public string Role { get; set; }

        public DateTime RegisteryTime { get; set; }

        public string AvatarUrl { get; set; }
        
        public DateTime ActiveTime { get; set; } = DateTime.UtcNow;
        
        public DateTime LastLoginTime { get; set; } = DateTime.UtcNow;

        public dynamic PassedProblems { get; set; }

        public dynamic TriedProblems { get; set; }
    }
}
