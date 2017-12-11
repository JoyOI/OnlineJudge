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

    public class Problem
    {
        public string id { get; set; }

        public int point { get; set; }

        public string number { get; set; }
    }

    public class Detail
    {
        public string problemId { get; set; }

        public int point { get; set; }

        public int point2 { get; set; }

        public int point3 { get; set; }

        public TimeSpan timeSpan { get; set; }

        public TimeSpan timeSpan2 { get; set; }

        public bool isAccepted { get; set; }

        public string display { get; set; }
    }

    public class Attendee
    {
        public Guid userId { get; set; }

        public bool isVirtual { get; set; }

        public int point => detail.Values.Sum(x => x.point);

        public string pointDisplay { get; set; }

        public int point2 => detail.Values.Sum(x => x.point2);

        public string point2Display { get; set; }

        public int point3 => detail.Values.Sum(x => x.point3);

        public string point3Display { get; set; }

        public TimeSpan timeSpan => new TimeSpan(detail.Values.Sum(x => x.timeSpan.Ticks));

        public string timeSpanDisplay { get; set; }

        public TimeSpan timeSpan2 => new TimeSpan(detail.Values.Sum(x => x.timeSpan2.Ticks));

        public string timeSpan2Display { get; set; }

        public IDictionary<string, Detail> detail { get; set; } = new Dictionary<string, Detail>();
    }

    public class Standings
    {
        public string id { get; set; }

        public string title { get; set; }

        public IDictionary<string, string> columnDefinations { get; set; }

        public IEnumerable<Problem> problems { get; set; }

        public IEnumerable<Attendee> attendees { get; set; }
    }
}
