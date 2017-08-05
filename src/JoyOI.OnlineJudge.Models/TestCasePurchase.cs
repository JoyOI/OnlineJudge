using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class TestCasePurchase
    {
        [ForeignKey("TestCase")]
        public Guid TestCaseId { get; set; }

        public virtual TestCase TestCase { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime CreatedTime { get; set; }
    }
}
