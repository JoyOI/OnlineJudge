using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class OlympicInformationContestExecutor : DefaultContestExecutor
    {
        private Dictionary<Guid, TimeSpan> _cachedVirtualContestAttendees;

        private Dictionary<Guid, TimeSpan> CachedVirtualContestAttendees
        {
            get
            {
                if (_cachedVirtualContestAttendees == null)
                {
                    var ret = new Dictionary<Guid, TimeSpan>();
                    var result = DB.Attendees
                        .Where(x => x.ContestId == ContestId)
                        .Where(x => x.IsVirtual)
                        .Where(x => x.RegisterTime.Add(Contest.Duration) >= DateTime.UtcNow)
                        .Select(x => new KeyValuePair<Guid, TimeSpan>(x.UserId, x.RegisterTime.Add(Contest.Duration) - DateTime.UtcNow))
                        .ToList();

                    foreach (var x in result)
                    {
                        ret.Add(x.Key, x.Value);
                    }

                    _cachedVirtualContestAttendees = ret;
                }
                return _cachedVirtualContestAttendees;
            }
        }

        public override IDictionary<string, string> PointColumnDefinations => new Dictionary<string, string>
        {
            { "Point", "Score" },
            { "Point3", "Time" }
        };

        public override bool AllowFilterByJudgeResult => HasPermissionToContest();

        public override PushNotificationType PushNotificationSetting => PushNotificationType.All;

        public override void OnShowJudgeResult(JudgeStatus status)
        {
            if (status.SubStatuses != null)
            {
                status.SubStatuses.Clear();
                status.SubStatuses.Add(new SubJudgeStatus
                {
                    Result = JudgeResult.Hidden,
                    SubId = 1
                });
            }
            status.Hint = "You can not view the result now.";
            status.Result = JudgeResult.Hidden;
            status.TimeUsedInMs = 0;
            status.MemoryUsedInByte = 0;
        }

        public void OnShowStandings(Attendee attendee)
        {
            if (attendee.isVirtual)
            {
                if (!HasPermissionToContest() && CachedVirtualContestAttendees.ContainsKey(attendee.userId))
                {
                    attendee.detail.Clear();
                    attendee.IsInvisible = true;
                    attendee.InvisibleDisplay = "模拟赛进行中，剩余时间：" + CachedVirtualContestAttendees[attendee.userId].ToString("d\\.hh\\:mm\\:ss");
                }
            }
        }

        public override void OnJudgeCompleted(JudgeStatus status)
        {
            var attendee = DB.Attendees
                .Single(x => x.ContestId == status.ContestId && x.UserId == status.UserId);

            var cpls = DB.ContestProblemLastStatuses
                .Include(x => x.Status)
                .SingleOrDefault(x => x.ProblemId == status.ProblemId && x.UserId == status.UserId && x.ContestId == status.ContestId);

            if (cpls != null && status.CreatedTime <= cpls.Status.CreatedTime)
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
                    Point = status.SubStatuses == null ? 0 : status.SubStatuses.Count(x => x.Result == JudgeResult.Accepted) * contestProblem.Point / status.SubStatuses.Count,
                    Point3 = status.TimeUsedInMs,
                    IsAccepted = status.Result == JudgeResult.Accepted,
                    IsVirtual = attendee.IsVirtual
                };
                DB.ContestProblemLastStatuses.Add(cpls);
            }
            else
            {
                cpls.StatusId = status.Id;
                cpls.Point = status.SubStatuses == null ? 0 : status.SubStatuses.Count(x => x.Result == JudgeResult.Accepted) * contestProblem.Point / status.SubStatuses.Count;
                cpls.Point3 = status.TimeUsedInMs;
                cpls.IsAccepted = status.Result == JudgeResult.Accepted;
            }
            DB.SaveChanges();
        }

        public override bool IsAvailableToGetStandings(string username = null)
        {
            if (IsContestInProgress(username))
                return false;
            else
                return true;
        }

        public override void GenerateProblemScoreDisplayText(Attendee src)
        {
            foreach (var x in src.detail.Values)
            {
                x.display = x.point.ToString();
            }
        }

        public override void GenerateTotalScoreDisplayText(Attendee src)
        {
            src.pointDisplay = src.point.ToString();
            if (src.point3 < 10000)
            {
                src.point3Display = src.point3 + " ms";
            }
            else
            {
                src.point3Display = (src.point3 / 1000.0).ToString("0.00") + " s";
            }
        }

        public override bool IsStandingsAvailable(string username = null)
        {
            return !IsContestInProgress(username) && Contest.Status != ContestStatus.Pending;
        }

        public override string GenerateProblemStatusText(string problemId, string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null)
            {
                return null;
            }

            var cpls = DB.ContestProblemLastStatuses.SingleOrDefault(x => x.ContestId == ContestId && x.UserId == user.Id&& x.ProblemId == problemId);
            if (cpls == null) {
                return null;
            } else
            {
                if (IsContestInProgress(username))
                {
                    return "Submitted";
                }
                else
                {
                    return cpls.Point.ToString();
                }
            }
        }

        public override async Task<IEnumerable<Attendee>> GenerateFullStandingsAsync(bool includingVirtual = true, CancellationToken token =default(CancellationToken))
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
                this.OnShowStandings(x);
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
                isVirtual = statuses.First().IsVirtual,
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

            this.OnShowStandings(ret);
            this.GenerateTotalScoreDisplayText(ret);
            this.GenerateProblemScoreDisplayText(ret);

            return ret;
        }
    }
}
