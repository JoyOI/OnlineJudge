using System;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public interface IContestExecutor
    {
        string ContestId { get; set; }

        bool AllowFilterByJudgeResult { get; }

        bool AllowFilterByHackResult { get; }

        bool IsContestInProgress(string username = null);

        void HandleJudgeResult(JudgeStatus status);

        void HandleHackResult(HackStatus status);
    }
}
