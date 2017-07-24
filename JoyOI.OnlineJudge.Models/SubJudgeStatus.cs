using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text;

namespace JoyOI.OnlineJudge.Models
{
    public class SubJudgeStatus
    {
        public Guid Id { get; set; }

        [ForeignKey("Status")]
        public Guid StatusId { get; set; }

        public virtual JudgeStatus Status { get; set; }

        [MaxLength(32)]
        public string Result { get; set; }

        [ForeignKey("TestCase")]
        public Guid TestCaseId { get; set; }

        public virtual TestCase TestCase { get; set; }
    }
}
