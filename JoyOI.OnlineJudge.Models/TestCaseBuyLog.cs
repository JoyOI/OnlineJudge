using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class TestCaseBuyLog
    {
        [ForeignKey("TestCase")]
        public Guid TestCaseId { get; set; }

        public virtual TestCase TestCase { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        public DateTime Time { get; set; }
    }
}
