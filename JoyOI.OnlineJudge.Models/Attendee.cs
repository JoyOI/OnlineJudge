using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text;

namespace JoyOI.OnlineJudge.Models
{
    public class Attendee
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        /// <summary>
        /// 竞赛ID
        /// </summary>
        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        public virtual Contest Contest { get; set; }

        /// <summary>
        /// 参赛时间
        /// </summary>
        public DateTime RegisterTime { get; set; }


        /// <summary>
        /// 是否为模拟赛
        /// </summary>
        public bool IsVirtual { get; set; }
    }
}
