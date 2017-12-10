using System;
using System.Collections.Generic;
using System.Linq;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class ContestExecutorFactory
    {
        private OnlineJudgeContext _db;

        public ContestExecutorFactory(OnlineJudgeContext db)
        {
            this._db = db;
        }

        public IContestExecutor Create(string contestId)
        {
            var contest = _db.Contests.SingleOrDefault(x => x.Id == contestId);
            if (contest == null)
                throw new KeyNotFoundException("Contest not found, id=" + contestId);
            switch (contest.Type)
            {
                case ContestType.OI:
                    return new OlympicInformationContestExecutor() { DB = this._db, ContestId = contestId };
                default:
                    throw new NotSupportedException($"The contest type { contest.Type } is not supported yet.");
            }
        }
    }
}
