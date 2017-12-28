using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.OnlineJudge.Models
{
    public class GroupContestReference
    {
        [MaxLength(128)]
        [ForeignKey("Group")]
        public string GroupId { get; set; }

        public virtual Group Group { get; set; }

        [MaxLength(128)]
        [ForeignKey("Contest")]
        public string ContestId { get; set; }

        public virtual Contest Contest { get; set; }
    }
}
