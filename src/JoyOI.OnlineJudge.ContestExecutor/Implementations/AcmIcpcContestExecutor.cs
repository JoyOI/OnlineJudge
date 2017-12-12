using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class AcmIcpcContestExecutor : DefaultContestExecutor
    {
        public override IDictionary<string, string> PointColumnDefinations => new Dictionary<string, string>
        {
            { "Point", "AC" },
            { "TimeSpan", "Penalty" }
        };

        public override bool AllowFilterByJudgeResult => true;

        public override PushNotificationType PushNotificationSetting => PushNotificationType.All;

        public override void OnShowJudgeResult(JudgeStatus status)
        {
            if (status.SubStatuses != null)
            {
                status.SubStatuses.Clear();
                status.SubStatuses.Add(new SubJudgeStatus
                {
                    Result = status.Result,
                    SubId = 1,
                    Hint = "ACM/ICPC 赛制不支持查看测试点详情"
                });
            }
        }

        public override void OnJudgeCompleted(JudgeStatus status)
        {
            var attendee = DB.Attendees
                .Single(x => x.ContestId == status.ContestId && x.UserId == status.UserId);

            var cpls = DB.ContestProblemLastStatuses
                .Include(x => x.Status)
                .SingleOrDefault(x => x.ProblemId == status.ProblemId && x.UserId == status.UserId && x.ContestId == status.ContestId);

            if (cpls != null && cpls.Status.Result == JudgeResult.Accepted || status.Result == JudgeResult.CompileError || status.Result == JudgeResult.SystemError)
            {
                return;
            }

            var contestProblem = DB.ContestProblems.Single(x => x.ContestId == status.ContestId && x.ProblemId == status.ProblemId);
            if (cpls == null)
            {
                cpls = new ContestProblemLastStatus
                {
                    ProblemId = status.ProblemId,
                    ContestId = status.ContestId,
                    UserId = status.UserId,
                    StatusId = status.Id,
                    Point = status.Result == JudgeResult.Accepted ? 1 : 0,
                    Point3 = status.Result == JudgeResult.Accepted ? 0 : 1,
                    TimeSpan = status.Result == JudgeResult.Accepted ? ComputeTimeSpan(status.CreatedTime, status.UserId) : default(TimeSpan),
                    IsAccepted = status.Result == JudgeResult.Accepted,
                    IsVirtual = attendee.IsVirtual
                };
                DB.ContestProblemLastStatuses.Add(cpls);
            }
            else
            {
                cpls.StatusId = status.Id;
                cpls.Point = status.Result == JudgeResult.Accepted ? 1 : 0;
                cpls.Point3 += status.Result == JudgeResult.Accepted ? 0 : 1;
                cpls.TimeSpan = status.Result == JudgeResult.Accepted ? ComputeTimeSpan(status.CreatedTime, status.UserId).Add(new TimeSpan(0, 20 * cpls.Point3, 0)) : default(TimeSpan);
                cpls.IsAccepted = status.Result == JudgeResult.Accepted;
            }
            DB.SaveChanges();
        }

        public override bool IsAvailableToGetStandings(string username = null) => true;

        public override void GenerateProblemScoreDisplayText(Attendee src)
        {
            foreach (var x in src.detail.Values)
            {
                if (x.isAccepted)
                {
                    x.display = x.timeSpan.ToString();
                    if (x.point3 != 0)
                    {
                        x.display += $"\n(-{x.point3})";
                    }
                }
                else if (x.point3 != 0)
                {
                    x.display = $"(-{x.point3})";
                }
            }
        }

        public override void GenerateTotalScoreDisplayText(Attendee src)
        {
            src.pointDisplay = src.point.ToString();
            src.timeSpanDisplay = src.timeSpan.ToString();
        }

        public override bool IsStandingsAvailable(string username = null)
        {
            return Contest.Status != ContestStatus.Pending;
        }

        public override string GenerateProblemStatusText(string problemId, string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null)
            {
                return null;
            }

            var cpls = DB.ContestProblemLastStatuses.SingleOrDefault(x => x.ContestId == ContestId && x.UserId == user.Id && x.ProblemId == problemId);
            if (cpls == null)
            {
                return null;
            }
            else
            {
                if (cpls.IsAccepted)
                {
                    return "Accepted";
                }
                else
                {
                    return $"{cpls.Point3} Fails";
                }
            }
        }

        private TimeSpan ComputeTimeSpan(DateTime? statusTime = null, Guid? userId = null)
        {
            if (statusTime == null)
            {
                statusTime = DateTime.UtcNow;
            }

            if (userId == null)
            {
                userId = User.Current == null ? null : (Guid?)User.Current.Id;
            }
            
            if (userId == null)
            {
                return statusTime.Value - Contest.Begin;
            }

            var attendee = DB.Attendees.SingleOrDefault(x => x.UserId == userId && x.ContestId == ContestId);
            if (attendee == null)
            {
                return statusTime.Value - Contest.Begin;
            }
            else
            {
                return statusTime.Value - (attendee.IsVirtual ? attendee.RegisterTime : Contest.Begin);
            }
        }
    }
}
