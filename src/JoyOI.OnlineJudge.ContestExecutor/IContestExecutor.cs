using System;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public interface IContestExecutor
    {
        string ContestId { get; set; }

        bool AllowFilterByJudgeResult { get; }

        bool AllowFilterByHackResult { get; }

        bool AllowJudgeFinishedPushNotification { get; }

        bool AllowHackFinishedPushNotification { get; }

        bool IsContestInProgress(string username = null);

        bool IsAvailableToGetStandings(string username = null);

        void OnShowJudgeResult(JudgeStatus status);

        void OnShowHackResult(HackStatus status);

        void OnJudgeCompleted(JudgeStatus status);

        void OnHackCompleted(HackStatus status);
    }
}
