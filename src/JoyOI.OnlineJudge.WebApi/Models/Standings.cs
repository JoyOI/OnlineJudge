using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi.Models
{
    public class Standings
    {
        public Guid UserId { get; set; }

        public int Point { get; set; }

        public int Point2 { get; set; }

        public int Point3 { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public TimeSpan TimeSpan2 { get; set; }

        public List<ContestProblemLastStatus> Statuses { get; set; }
    }
}
