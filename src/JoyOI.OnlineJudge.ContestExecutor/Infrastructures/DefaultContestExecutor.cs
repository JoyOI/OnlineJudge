using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public abstract class DefaultContestExecutor : IContestExecutor
    {
        public OnlineJudgeContext DB;
        public SmartUser<User, Guid> User;

        public string ContestId { get; set; }

        private Contest _contest;

        public Contest Contest
        {
            get
            {
                if (_contest == null)
                {
                    _contest = DB.Contests.Single(x => x.Id == ContestId);
                }
                return _contest;
            }
        }

        public virtual bool AllowFilterByJudgeResult => true;

        public virtual bool AllowFilterByHackResult => true;

        public virtual PushNotificationType PushNotificationSetting => PushNotificationType.All;

        public virtual bool AllowHackFinishedPushNotification => true;

        public virtual IDictionary<string, string> PointColumnDefinations => new Dictionary<string, string>();

        public virtual void OnShowHackResult(HackStatus status)
        {
        }

        public virtual void OnShowJudgeResult(JudgeStatus status)
        {
        }

        public bool IsContestInProgress(string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null)
            {
                return Contest.Status == ContestStatus.Live;
            }
            var attendee = DB.Attendees.SingleOrDefault(x => x.UserId == user.Id && x.ContestId == ContestId);
            if (username == null || attendee == null || !attendee.IsVirtual)
            {
                return Contest.Status == ContestStatus.Live;
            }
            else
            {
                return attendee.RegisterTime.Add(Contest.Duration) > DateTime.UtcNow;
            }
        }

        public bool IsContestEnded(string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null)
            {
                return Contest.Status == ContestStatus.Done;
            }
            var attendee = DB.Attendees.SingleOrDefault(x => x.UserId == user.Id && x.ContestId == ContestId);
            if (username == null || attendee == null || !attendee.IsVirtual)
            {
                return Contest.Status == ContestStatus.Done;
            }
            else
            {
                return attendee.RegisterTime.Add(Contest.Duration) < DateTime.UtcNow;
            }
        }

        public virtual void OnJudgeCompleted(JudgeStatus status)
        {
            throw new NotImplementedException();
        }

        public virtual void OnHackCompleted(HackStatus status)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsAvailableToGetStandings(string username = null)
        {
            return true;
        }

        public virtual void GenerateTotalScoreDisplayText(Attendee src)
        {
        }

        public virtual void GenerateProblemScoreDisplayText(Attendee src)
        {
        }

        public virtual bool IsStandingsAvailable(string username = null)
        {
            return true;
        }

        public virtual string GenerateProblemStatusText(string problemId, string username = null)
        {
            return null;
        }

        public virtual bool HasPermissionToContest(string username = null)
        {
            var user = GetSpecifiedOrCurrentUser(username);
            if (user == null)
                return false;

            if (User.Manager.IsInAnyRolesAsync(user, Constants.MasterOrHigherRoles).Result)
                return true;
            if (!DB.UserClaims.Any(x => x.UserId == user.Id
                   && x.ClaimType == Constants.ContestEditPermission
                   && x.ClaimValue == ContestId))
                return true;
            return false;
        }

        protected virtual User GetSpecifiedOrCurrentUser(string username = null)
        {
            if (User.Current == null && string.IsNullOrEmpty(username))
                return null;
            return string.IsNullOrEmpty(username) ? User.Current : DB.Users.SingleOrDefault(x => x.UserName == username);
        }

        public virtual IEnumerable<string> GetContestOwners()
        {
            var ids = DB.UserClaims
                .Where(x => x.ClaimType == Constants.ContestEditPermission && x.ClaimValue == ContestId)
                .Select(x => x.UserId);

            return DB.Users
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.UserName)
                .ToList();
        }

        public virtual bool IsStatusHackable(JudgeStatus status) => false;

        public virtual bool IsAbleToSubmitProblem(string problemId, string username = null) => true;

        public virtual Task<IEnumerable<Attendee>> GenerateFullStandingsAsync(bool includeVirtual = true, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public virtual Task<Attendee> GenerateSingleStandingsAsync(string username = null, CancellationToken token = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
