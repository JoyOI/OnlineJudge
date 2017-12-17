using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using JoyOI.OnlineJudge.Models;
using JoyOI.ManagementService.SDK;

namespace JoyOI.OnlineJudge.ContestExecutor
{
    public class ContestExecutorFactory : IDisposable
    {
        private OnlineJudgeContext _db;
        private SmartUser<User, Guid> _user;
        private ManagementServiceClient _mgmt;
        private IConfiguration _config;

        public ContestExecutorFactory(OnlineJudgeContext db, SmartUser<User, Guid> user, ManagementServiceClient mgmt, IConfiguration config)
        {
            this._db = db;
            this._user = user;
            this._mgmt = mgmt;
            this._config = config;
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
                case ContestType.Codeforces:
                    return new CodeforcesContestExecutor()
                    {
                        DB = this._db,
                        User = this._user,
                        ManagementService = this._mgmt,
                        Configuration = this._config,
                        ContestId = contestId
                    };
                default:
                    throw new NotSupportedException($"The contest type { contest.Type } is not supported yet.");
            }
        }

        public void Dispose()
        {
            this._db.Dispose();
        }
    }
}
