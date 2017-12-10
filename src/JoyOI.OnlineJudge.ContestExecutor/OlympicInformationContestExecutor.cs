using System;
using System.Collections.Generic;
using System.Text;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class OlympicInformationContestExecutor : DefaultContestExecutor
    {
        public override bool AllowFilterByJudgeResult => false;

        public override void HandleJudgeResult(JudgeStatus status)
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
            status.Result = JudgeResult.Hidden;
            status.TimeUsedInMs = 0;
            status.MemoryUsedInByte = 0;
        }
    }
}
