using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public interface IContestExecutor
    {
        IDictionary<string, string> PointColumnDefinations { get; }

        string ContestId { get; set; }

        bool AllowFilterByJudgeResult { get; }

        bool AllowFilterByHackResult { get; }

        bool AllowLockProblem { get; }

        PushNotificationType PushNotificationSetting { get; }

        bool AllowHackFinishedPushNotification { get; }

        bool IsContestInProgress(string username = null);

        bool IsAvailableToGetStandings(string username = null);

        bool IsStandingsAvailable(string username = null);

        void OnShowJudgeResult(JudgeStatus status);

        void OnShowHackResult(HackStatus status);

        void OnJudgeCompleted(JudgeStatus status);

        void OnHackCompleted(HackStatus status);

        Task<IEnumerable<Attendee>> GenerateFullStandingsAsync(bool includeVirtual = true, CancellationToken token = default(CancellationToken));

        Task<Attendee> GenerateSingleStandingsAsync(string username = null, CancellationToken token = default(CancellationToken));

        void GenerateTotalScoreDisplayText(Attendee src);

        void GenerateProblemScoreDisplayText(Attendee src);

        string GenerateProblemStatusText(string problemId, string username = null);

        bool HasPermissionToContest(string username = null);

        bool IsAbleToSubmitProblem(string problemId, string username = null);

        bool IsStatusHackable(JudgeStatus status);

        IEnumerable<string> GetContestOwners();
    }
}
