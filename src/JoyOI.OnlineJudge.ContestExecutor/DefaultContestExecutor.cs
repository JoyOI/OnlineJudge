using System;
using System.Linq;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public abstract class DefaultContestExecutor : IContestExecutor
    {
        public OnlineJudgeContext DB;

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

        public virtual bool AllowJudgeFinishedPushNotification => true;

        public virtual bool AllowHackFinishedPushNotification => true;

        public virtual void OnShowHackResult(HackStatus status)
        {
        }

        public virtual void OnShowJudgeResult(JudgeStatus status)
        {
        }

        public bool IsContestInProgress(string username = null)
        {
            var attendee = DB.Attendees.SingleOrDefault(x => x.User.UserName == username && x.ContestId == ContestId);
            if (username == null || attendee == null || !attendee.IsVirtual)
            {
                return Contest.Status == ContestStatus.Live;
            }
            else
            {
                return attendee.RegisterTime.Add(Contest.Duration) > DateTime.UtcNow;
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
    }
}
