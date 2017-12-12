using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class ContestExecutorFactory
    {
        private OnlineJudgeContext _db;
        private SmartUser<User, Guid> _user;

        public ContestExecutorFactory(OnlineJudgeContext db, SmartUser<User, Guid> user)
        {
            this._db = db;
            this._user = user;
        }

        public IContestExecutor Create(string contestId)
        {
            var contest = _db.Contests.SingleOrDefault(x => x.Id == contestId);
            if (contest == null)
                throw new KeyNotFoundException("Contest not found, id=" + contestId);
            switch (contest.Type)
            {
                case ContestType.OI:
                    return new OlympicInformationContestExecutor()
                    {
                        DB = this._db,
                        User = this._user,
                        ContestId = contestId
                    };
                case ContestType.ACM:
                    return new AcmIcpcContestExecutor()
                    {
                        DB = this._db,
                        User = this._user,
                        ContestId = contestId
                    };
                default:
                    throw new NotSupportedException($"The contest type { contest.Type } is not supported yet.");
            }
        }
    }
}
