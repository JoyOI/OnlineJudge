using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class OlympicInformationContestExecutor : DefaultContestExecutor
    {
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
                    Point3 = status.TimeUsedInMs
                };
                DB.ContestProblemLastStatuses.Add(cpls);
            }
            else
            {
                cpls.StatusId = status.Id;
                cpls.Point = status.SubStatuses == null ? 0 : status.SubStatuses.Count(x => x.Result == JudgeResult.Accepted) * contestProblem.Point / status.SubStatuses.Count;
                cpls.Point3 = status.TimeUsedInMs;
            }
            DB.SaveChanges();
        }
    }
}
