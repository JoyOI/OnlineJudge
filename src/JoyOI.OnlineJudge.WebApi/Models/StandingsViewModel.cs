using System;
using System.Collections.Generic;
using System.Linq; 

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class StandingsProblemViewModel
    {
        public string id { get; set; }

        public int point { get; set; }

        public string number { get; set; }
    }

    public class StandingsProblemDetailViewModel
    {
        public string problemId { get; set; }

        public int point { get; set; }

        public int point2 { get; set; }

        public int point3 { get; set; }

        public TimeSpan timeSpan { get; set; }

        public TimeSpan timeSpan2 { get; set; }

        public bool isAccepted { get; set; }
    }

    public class StandingsAttendeeViewModel
    {
        public Guid userId { get; set; }

        public bool isVirtual { get; set; }

        public int point => detail.Values.Sum(x => x.point);

        public int point2 => detail.Values.Sum(x => x.point2);

        public int point3 => detail.Values.Sum(x => x.point3);

        public TimeSpan timeSpan => new TimeSpan(detail.Values.Sum(x => x.timeSpan.Ticks));

        public TimeSpan timeSpan2 => new TimeSpan(detail.Values.Sum(x => x.timeSpan2.Ticks));

        public IDictionary<string, StandingsProblemDetailViewModel> detail { get; set; } = new Dictionary<string, StandingsProblemDetailViewModel>();
    }

    public class StandingsViewModel
    {
        public string id { get; set; }

        public string title { get; set; }

        public IEnumerable<StandingsProblemViewModel> problems { get; set; }

        public IEnumerable<StandingsAttendeeViewModel> attendees { get; set; }
    }
}
