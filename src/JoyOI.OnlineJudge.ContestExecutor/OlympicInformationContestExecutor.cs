using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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

        public override bool AllowFilterByJudgeResult => false;

        public override bool AllowJudgeFinishedPushNotification => false;

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
            var attendee = DB.Attendees.Single(x => x.ContestId == status.ContestId && x.UserId == status.UserId);
            var cpls = DB.ContestProblemLastStatuses.SingleOrDefault(x => x.ProblemId == status.ProblemId && x.UserId == status.UserId && x.ContestId == status.ContestId);
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
            src.point3Display = src.point3 + " ms";
        }
    }
}
