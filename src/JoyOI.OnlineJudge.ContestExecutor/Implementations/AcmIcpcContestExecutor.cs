using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public override async Task<IEnumerable<Attendee>> GenerateFullStandingsAsync(bool includingVirtual = true, CancellationToken token = default(CancellationToken))
        {
            var exceptNestedQuery = DB.Attendees
               .Where(x => x.ContestId == ContestId)
               .Where(x => x.IsVirtual)
               .Select(x => x.UserId);
            IQueryable<ContestProblemLastStatus> query = DB.ContestProblemLastStatuses
                .Where(x => x.ContestId == ContestId);
            if (!includingVirtual)
                query = query.Where(x => !exceptNestedQuery.Contains(x.UserId));
            var attendees = (await query
               .GroupBy(x => new { x.UserId, x.IsVirtual })
               .ToListAsync(token))
               .Select(x => new Attendee
               {
                   userId = x.Key.UserId,
                   isVirtual = x.Key.IsVirtual,
                   detail = x.GroupBy(y => y.ProblemId)
                       .Select(y => new Detail
                       {
                           problemId = y.Key,
                           isAccepted = y.First().IsAccepted,
                           isHackable = x.First().IsHackable,
                           point = y.First().Point,
                           point2 = y.First().Point2,
                           point3 = y.First().Point3,
                           point4 = y.First().Point4,
                           statusId = y.First().StatusId.ToString(),
                           timeSpan = y.First().TimeSpan,
                           timeSpan2 = y.First().TimeSpan2
                       })
                       .ToDictionary(y => y.problemId)
               })
               .OrderByDescending(x => x.point)
               .ThenByDescending(x => x.point2)
               .ThenBy(x => x.point3)
               .ThenBy(x => x.point4)
               .ThenBy(x => x.timeSpan)
               .ThenBy(x => x.timeSpan2)
               .ToList();

            foreach (var x in attendees)
            {
                this.GenerateProblemScoreDisplayText(x);
                this.GenerateTotalScoreDisplayText(x);
            }

            return attendees
               .OrderBy(x => x.IsInvisible);
        }

        public override async Task<Attendee> GenerateSingleStandingsAsync(string username = null, CancellationToken token = default(CancellationToken))
        {
            if (username == null && User.Current != null)
            {
                username = User.Current.UserName;
            }

            var statuses = await DB.ContestProblemLastStatuses
                .Where(x => x.ContestId == ContestId)
                .Where(x => x.User.UserName == username)
                .ToListAsync(token);

            if (statuses.Count == 0)
                return null;

            var ret = new Attendee
            {
                userId = statuses.First().UserId,
                detail = statuses
                    .GroupBy(x => x.ProblemId)
                    .Select(x => new Detail
                    {
                        isAccepted = x.First().IsAccepted,
                        isHackable = x.First().IsHackable,
                        point = x.First().Point,
                        point2 = x.First().Point2,
                        point3 = x.First().Point3,
                        point4 = x.First().Point4,
                        timeSpan = x.First().TimeSpan,
                        timeSpan2 = x.First().TimeSpan2,
                        problemId = x.First().ProblemId,
                        statusId = x.First().StatusId.ToString()
                    })
                    .ToDictionary(x => x.problemId)
            };

            this.GenerateTotalScoreDisplayText(ret);
            this.GenerateProblemScoreDisplayText(ret);

            return ret;
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
