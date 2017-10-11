using System;
using System.Collections.Generic;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class UserViewModel
    {
        public Guid id { get; set; }

        public string username { get; set; }

        public string role { get; set; }

        public string avatarUrl { get; set; }

        public string motto { get; set; }

        public DateTime registeryTime { get; set; }

        public DateTime activeTime { get; set; }

        public IEnumerable<string> triedProblems { get; set; }

        public IEnumerable<string> passedProblems { get; set; }

        public IEnumerable<string> joinedGroups { get; set; }

        public IEnumerable<string> uploadedProblems { get; set; }
    }
}
