using System.Collections.Generic;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class VirtualJudgeResult
    {
        public string Result { get; set; }
        public long TimeUsedInMs { get; set; }
        public long MemoryUsedInByte { get; set; }
        public string Hint { get; set; }

        public IEnumerable<VirtualJudgeSubStatus> SubStatuses { get; set; }
    }

    public class VirtualJudgeSubStatus
    {
        public int SubId { get; set; }

        public string Result { get; set; }

        public long TimeUsedInMs { get; set; }

        public long MemoryUsedInByte { get; set; }

        public string Hint { get; set; }
    }
}
