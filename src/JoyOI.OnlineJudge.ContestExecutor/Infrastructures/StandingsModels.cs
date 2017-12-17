using System;
using System.Collections.Generic;
using System.Linq;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public enum PushNotificationType
    {
        None,
        Master,
        All
    }

    public class ProblemSummary
    {
        public string id { get; set; }

        public int point { get; set; }

        public string number { get; set; }
    }

    public class Detail
    {
        public string problemId { get; set; }

        public string statusId { get; set; }

        public int point { get; set; }

        public int point2 { get; set; }

        public int point3 { get; set; }

        public int point4 { get; set; }

        public TimeSpan timeSpan { get; set; }

        public TimeSpan timeSpan2 { get; set; }

        public bool isAccepted { get; set; }

        public bool isHackable { get; set; }

        public string display { get; set; }
    }

    public class Attendee
    {
        public Guid userId { get; set; }

        public bool isVirtual { get; set; }

        public virtual int point => detail.Values.Sum(x => x.point);

        public string pointDisplay { get; set; }

        public virtual int point2 => detail.Values.Sum(x => x.point2);

        public string point2Display { get; set; }

        public virtual int point3 => detail.Values.Sum(x => x.point3);

        public string point3Display { get; set; }
        
        public virtual int point4 => detail.Values.Sum(x => x.point3);

        public string point4Display { get; set; }

        public virtual TimeSpan timeSpan => new TimeSpan(detail.Values.Sum(x => x.timeSpan.Ticks));

        public string timeSpanDisplay { get; set; }

        public virtual TimeSpan timeSpan2 => new TimeSpan(detail.Values.Sum(x => x.timeSpan2.Ticks));

        public string timeSpan2Display { get; set; }

        public IDictionary<string, Detail> detail { get; set; } = new Dictionary<string, Detail>();

        public bool IsInvisible { get; set; }

        public string InvisibleDisplay { get; set; }
    }

    public class CodeforcesAttendee : Attendee
    {
        public override int point => base.point + (base.point2 + base.point3) * 100 - base.point3 * 50;
    }

    public class Standings
    {
        public string id { get; set; }

        public string title { get; set; }

        public IDictionary<string, string> columnDefinations { get; set; }

        public IEnumerable<ProblemSummary> problems { get; set; }

        public IEnumerable<Attendee> attendees { get; set; }
    }
}
