using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using JoyOI.ManagementService.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    public class JudgeController : BaseController
    {
        [HttpGet("all")]
        public async Task<ApiResult<PagedResult<IEnumerable<JudgeStatus>>>> Get(string problemId, JudgeResult? status, Guid? userId, string contestId, string language, int? page, CancellationToken token)
        {
            IQueryable<JudgeStatus> ret = DB.JudgeStatuses;

            if (!string.IsNullOrWhiteSpace(problemId))
            {
                ret = ret.Where(x => x.ProblemId == problemId);
            }

            if (status.HasValue)
            {
                ret = ret.Where(x => x.Result == status.Value);
            }

            if (userId.HasValue)
            {
                ret = ret.Where(x => x.UserId == userId);
            }

            if (!string.IsNullOrWhiteSpace(contestId))
            {
                ret = ret.Where(x => x.ContestId == contestId);
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                ret = ret.Where(x => x.Language == language);
            } 

            var result = await Paged(ret.OrderByDescending(x => x.CreatedTime), page ?? 1, 50, token);
            if (!IsMasterOrHigher && result.data.result.Any(x => !string.IsNullOrWhiteSpace(x.ContestId)))
            {
                var tasks = new List<Task>(13);
                var pendingRemove = new ConcurrentBag<JudgeStatus>();
                foreach (var x in result.data.result.Where(x => !string.IsNullOrWhiteSpace(x.ContestId)))
                {
                    tasks.Add(new Task(async () => {
                        var isOiInProgress = await DB.Contests.AnyAsync(y => y.Id == x.ContestId && y.Type == ContestType.OI && y.Begin >= DateTime.Now && y.End < DateTime.Now);
                        if (isOiInProgress && !await HasPermissionToContestAsync(x.ContestId, token) && !await HasPermissionToProblemAsync(x.ProblemId, token))
                        {
                            if (!status.HasValue)
                            {
                                x.Result = JudgeResult.Hidden;
                                x.TimeUsedInMs = 0;
                                x.MemoryUsedInByte = 0;
                            }
                            else
                            {
                                pendingRemove.Add(x);
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                foreach (var x in pendingRemove)
                {
                    (result.data.result as List<JudgeStatus>).Remove(x);
                }
            }

            var userIds = result.data.result.Select(x => x.UserId).Distinct();
            var users = await DB.Users
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new { x.Id, x.UserName })
                .ToDictionaryAsync(x => x.Id, x => x.UserName);

            foreach (var x in result.data.result)
            {
                x.User = new User { Id = x.UserId, UserName = users[x.Id] };
            }

            return result;
        }

        #region Private Functions
        private async Task<bool> HasPermissionToContestAsync(string contestId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ContestEditPermission
                   && x.ClaimValue == contestId));

        private async Task<bool> HasPermissionToProblemAsync(string problemId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == problemId));
        #endregion
    }
}
