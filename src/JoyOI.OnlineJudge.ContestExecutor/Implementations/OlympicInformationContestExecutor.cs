using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class OlympicInformationContestExecutor : DefaultContestExecutor
    {
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
    }
}
