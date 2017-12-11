using System.Collections.Generic;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public interface IContestExecutor
    {
        IDictionary<string, string> PointColumnDefinations { get; }

        string ContestId { get; set; }

        bool AllowFilterByJudgeResult { get; }

        bool AllowFilterByHackResult { get; }

        PushNotificationType PushNotificationSetting { get; }

        bool AllowHackFinishedPushNotification { get; }

        bool IsContestInProgress(string username = null);

        bool IsAvailableToGetStandings(string username = null);

        bool IsStandingsAvailable(string username = null);

        void OnShowJudgeResult(JudgeStatus status);

        void OnShowHackResult(HackStatus status);

        void OnJudgeCompleted(JudgeStatus status);

        void OnHackCompleted(HackStatus status);

        void GenerateTotalScoreDisplayText(Attendee src);

        void GenerateProblemScoreDisplayText(Attendee src);

        string GenerateProblemStatusText(string problemId, string username = null);

        bool HasPermissionToContest(string username = null);

        IEnumerable<string> GetContestOwners();
    }
}
