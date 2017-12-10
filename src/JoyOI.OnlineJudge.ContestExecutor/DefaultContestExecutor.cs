using System;
using System.Linq;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class DefaultContestExecutor : IContestExecutor
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

        public virtual bool AllowFilterByJudgeResult { get => true; }

        public virtual bool AllowFilterByHackResult { get => true; }

        public virtual void HandleHackResult(HackStatus status)
        {
        }

        public virtual void HandleJudgeResult(JudgeStatus status)
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
    }
}
