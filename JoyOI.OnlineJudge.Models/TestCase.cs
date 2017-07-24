using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class TestCase
    {
        public Guid Id { get; set; }

        public Guid InputId { get; set; } // Management Service Blob ID

        public Guid OutputId { get; set; }

        [MaxLength(128)]
        [ForeignKey("Problem")]
        public string ProblemId { get; set; }

        public virtual Problem Problem { get; set; }

        public TestCaseType Type { get; set; }
    }
}
