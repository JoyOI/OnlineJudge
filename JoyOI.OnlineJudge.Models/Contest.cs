using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public enum ContestType
    {
        OI,
        ACM,
        Codeforces,
        TopCoder
    }

    public enum AttendPermission
    {
        Everyone,
        Team,
        Password
    }

    public class Contest
    {
        [MaxLength(128)]
        public string Id { get; set; }

        [MaxLength(128)]
        public string Title { get; set; }

        public string Description { get; set; }

        public ContestType Type { get; set; }

        public DateTime Begin { get; set; }

        public TimeSpan Duration { get; set; }

        [NotMapped]
        public DateTime End => Begin.Add(Duration);

        public AttendPermission AttendPermission { get; set; }

        public string PasswordOrTeam { get; set; }

        [NotMapped]
        public string Password => PasswordOrTeam;

        [NotMapped]
        public Guid TeamId => Guid.Parse(Password);

        public bool DisableVirtual { get; set; }

        public JsonObject<List<string>> BannedLanguages { get; set; } = "[]";
    }
}
