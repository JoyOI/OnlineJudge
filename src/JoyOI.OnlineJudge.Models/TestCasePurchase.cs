using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class TestCasePurchase
    {
        /// <summary>
        /// Cost of test case per problem
        /// </summary>
        [NotMapped]
        public const int Cost = 100;

        [MaxLength(128)]
        [ForeignKey("Problem")]
        public string ProblemId{ get; set; }

        public virtual Problem Problem { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        [WebApi(FilterLevel.PatchDisabled | FilterLevel.PutDisabled)]
        public DateTime CreatedTime { get; set; }
    }
}
