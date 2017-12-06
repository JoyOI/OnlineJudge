using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ContestController : BaseController
    {
        #region Contest
        [HttpGet("all")]
        public Task<IActionResult> Get(
            string title, 
            ContestType? type, 
            AttendPermission? attendPermission, 
            bool? highlight,
            DateTime? begin, 
            DateTime? end,
            int? page,
            CancellationToken token)
        {
            IQueryable<Contest> ret = DB.Contests;
            if (!string.IsNullOrWhiteSpace(title))
            {
                ret = ret.Where(x => x.Title.Contains(title) || title.Contains(x.Title));
            }
            if (type.HasValue)
            {
                ret = ret.Where(x => x.Type == type.Value);
            }
            if (attendPermission.HasValue)
            {
                ret = ret.Where(x => x.AttendPermission == attendPermission.Value);
            }
            if (begin.HasValue)
            {
                ret = ret.Where(x => x.Begin >= begin.Value);
            }
            if (end.HasValue)
            {
                ret = ret.Where(x => x.Begin.Add(x.Duration) <= end.Value);
            }
            if (highlight.HasValue && highlight.Value)
            {
                ret = ret.Where(x => x.IsHighlighted);
            }
            if (!page.HasValue)
            {
                page = 1;
            }
            return Paged(ret, page.Value, 20, token);
        }

        [HttpGet("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Get(string id, CancellationToken token)
        {
            var ret = await DB.Contests
                .SingleOrDefaultAsync(x => x.Id == id, token);
            if (ret == null)
            {
                return Result<Contest>(404, "Not found");
            }
            else
            {
                FilterEntity(ret);
                return Result(ret);
            }
        }
        
        [HttpPost("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        [HttpPatch("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Patch(string id, CancellationToken token)
        {
            if (!await HasPermissionToContestAsync(id, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                var contest = await DB.Contests
                    .SingleOrDefaultAsync(x => x.Id == id, token);

                if (contest == null)
                {
                    return Result(404, "Contest not found");
                }
               
                var fields = PatchEntity(contest, RequestBody);
                if (fields.Any(x => x == nameof(contest.IsHighlighted)) && !IsMasterOrHigher)
                {
                    return Result(403, "You don't have the permission to set the contest highlight.");
                }

                if (fields.Contains(nameof(Contest.Begin)))
                {
                    if (contest.Begin < DateTime.Now)
                    {
                        return Result(400, "Invalid begin time");
                    }
                }

                if (fields.Contains(nameof(Contest.Domain)) && !string.IsNullOrWhiteSpace(contest.Domain) && !await DB.Contests.AnyAsync(x => x.Domain == contest.Domain, token))
                {
                    return Result(400, "The domain is already existed.");
                }

                await DB.SaveChangesAsync(token);

                return Result(200, "Patch succeeded");
            }
        }

        [HttpPut("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Put(string id, CancellationToken token)
        {
            if (await DB.Contests.AnyAsync(x => x.Id == id, token))
            {
                return Result(400, "The contest id is already exists.");
            }
            else
            {
                var contest = PutEntity<Contest>(RequestBody).Entity;
                if (contest.Begin < DateTime.Now)
                {
                    return Result(400, "The begin time is invalid.");
                }
                if (!string.IsNullOrWhiteSpace(contest.Domain) && await DB.Contests.AnyAsync(x => x.Domain == contest.Domain, token))
                {
                    return Result(400, "The domain is already existed.");
                }

                DB.Contests.Add(contest);
                DB.UserClaims.Add(new IdentityUserClaim<Guid> { ClaimType = Constants.ContestEditPermission, ClaimValue = id, UserId = User.Current.Id });
                await DB.SaveChangesAsync(token);
                return Result(200, "Put succeeded");
            }
        }

        [HttpDelete("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Delete(string id, CancellationToken token)
        {
            if (await DB.Contests.AnyAsync(x => x.Id == id, token))
            {
                return Result(400, "The contest id is already exists.");
            }
            else if (!await HasPermissionToContestAsync(id, token))
            {
                return Result(401, "No permission");
            }
            else
            {
                var contest = await DB.Contests.SingleOrDefaultAsync(x => x.Id == id, token);
                if (contest == null)
                {
                    return Result(404, "Contest not found");
                }
                else if (contest.Begin <= DateTime.Now)
                {
                    return Result(400, "Cannot remove a started contest.");
                }
                else
                {
                    await DB.UserClaims
                        .Where(x => x.ClaimType == Constants.ContestEditPermission)
                        .Where(x => x.ClaimValue == id)
                        .DeleteAsync(token);

                    return Result(200, "Delete succeeded");
                }
            }
        }
        #endregion

        #region Contest Problem
        [HttpGet("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/problem/all")]
        public async Task<IActionResult> GetContestProblems(string contestId, CancellationToken token)
        {
            var contest = await DB.Contests
                .Include(x => x.Problems)
                .SingleOrDefaultAsync(x => x.Id == contestId, token);

            if (contest == null)
            {
                return Result<IEnumerable<ContestProblemViewModel>>(404, "Contest not found");
            }
            else if (contest.Begin > DateTime.Now && !await HasPermissionToContestAsync(contestId, token))
            {
                return Result<IEnumerable<ContestProblemViewModel>>(400, "The contest has not started");
            }
            else
            {
                var ret = await DB.ContestProblems
                    .Where(x => x.ContestId == contestId)
                    .OrderBy(x => x.Number)
                    .ThenBy(x => x.Point)
                    .Select(x => new ContestProblemViewModel
                    {
                        problemId = x.ProblemId,
                        number = x.Number,
                        point = x.Point
                    })
                    .ToListAsync(token);

                if (User.IsSignedIn())
                {
                    foreach (var x in ret)
                    {
                        switch (contest.Type)
                        {
                            case ContestType.OI:
                                if (contest.Status == ContestStatus.Pending)
                                {
                                    x.status = null;
                                }
                                else if (contest.Status == ContestStatus.Live)
                                {
                                    x.status = (await DB.JudgeStatuses.AnyAsync(y => y.UserId == User.Current.Id && y.ProblemId == x.problemId && y.ContestId == contestId) ? "Submitted" : null);
                                }
                                else
                                {
                                    var status = await DB.JudgeStatuses
                                        .Include(y => y.SubStatuses)
                                        .LastOrDefaultAsync(y => y.UserId == User.Current.Id && y.ProblemId == x.problemId && y.ContestId == contestId);
                                    if (status != null)
                                    {
                                        var count = status.SubStatuses.Count;
                                        var ac = status.SubStatuses.Count(y => y.Result == JudgeResult.Accepted);
                                        var point = contest.Problems.Single(y => y.ProblemId == status.ProblemId).Point;
                                        x.status = Convert.ToInt32((float)point * (float)ac / (float)count).ToString();
                                    }
                                }
                                break;
                            default:
                                throw new NotImplementedException($"The contest type { contest.Type.ToString() } has not been supported.");
                        }
                    }
                }

                return Result<IEnumerable<ContestProblemViewModel>>(ret);
            }
        }

        [HttpPut("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/problem/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> PutContestProblem(string contestId, string problemId, CancellationToken token)
        {
            var contest = await DB.Contests
                .SingleOrDefaultAsync(x => x.Id == contestId, token);
            var problem = await DB.Problems
                .SingleOrDefaultAsync(x => x.Id == problemId, token);
            var problemCount = await DB.ContestProblems.CountAsync(x => x.ContestId == contestId);
            if (contest == null)
            {
                return Result(404, "Contest not found");
            }
            else if (problem == null)
            {
                return Result(404, "Problem not found");
            }
            else if (problemCount >= 26)
            {
                return Result(400, "The contest problem count cannot be greater than 26");
            }
            else if (!await HasPermissionToContestAsync(contestId, token))
            {
                return Result(401, "No permission to this contest");
            }
            else if (!problem.IsVisible && !await HasPermissionToProblemAsync(contestId, token))
            {
                return Result(401, "No permission to this problem");
            }
            else if (await DB.ContestProblems.AnyAsync(x => x.ContestId == contestId && x.ProblemId == problemId, token))
            {
                return Result(400, "The problem has been already added into this contest");
            }
            else
            {
                var contestProblem = PutEntity<ContestProblem>(RequestBody);
                contestProblem.Entity.ContestId = contestId;
                contestProblem.Entity.ProblemId = problemId;

                if (contestProblem.Fields.Contains("Number"))
                {
                    if (await DB.ContestProblems.AnyAsync(x => x.ContestId == contestId && x.Number == contestProblem.Entity.Number, token))
                    {
                        return Result(400, "The problem number is already existed.");
                    }
                }
                else
                {
                    contestProblem.Entity.Number = ProblemNumberString[problemCount].ToString();
                }

                DB.ContestProblems.Add(contestProblem.Entity);
                await DB.SaveChangesAsync(token);
                return Result(200, "Put succeeded");
            }
        }

        [HttpPost("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/problem/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        [HttpPatch("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/problem/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> PatchContestProblem(string contestId, string problemId, [FromBody] string value, CancellationToken token)
        {
            var contestProblem = await DB.ContestProblems
                .SingleOrDefaultAsync(x => x.ContestId == contestId && x.ProblemId == problemId, token);
            if (contestProblem == null)
            {
                return Result(404, "Contest problem not found");
            }
            else if (!await HasPermissionToContestAsync(contestId, token))
            {
                return Result(401, "No permission to this contest");
            }
            else
            {
                var fields = PatchEntity(contestProblem, value);
                if (fields.Contains(nameof(ContestProblem.Number)))
                {
                    if (await DB.ContestProblems.AnyAsync(x => x.ContestId == contestId && x.Number == contestProblem.Number))
                    {
                        return Result(400, "The problem number is already existed.");
                    }
                }
                await DB.SaveChangesAsync(token);
                return Result(200, "Patch succeeded");
            }
        }

        [HttpDelete("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/problem/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> DeleteContestProblem(string contestId, string problemId, [FromBody] string value, CancellationToken token)
        {
            var contestProblem = await DB.ContestProblems
                .SingleOrDefaultAsync(x => x.ContestId == contestId && x.ProblemId == problemId, token);
            if (contestProblem == null)
            {
                return Result(404, "Contest problem not found");
            }
            else if (!await HasPermissionToContestAsync(contestId, token))
            {
                return Result(401, "No permission to this contest");
            }
            else
            {
                await DB.JudgeStatuses
                    .Where(x => x.ContestId == contestId)
                    .Where(x => x.ProblemId == problemId)
                    .DeleteAsync(token);

                await DB.ContestProblems
                    .Where(x => x.ProblemId == problemId)
                    .Where(x => x.ContestId == contestId)
                    .DeleteAsync(token);

                return Result(200, "Delete succeeded");
            }
        }
        #endregion

        #region Register
        [HttpGet("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/register")]
        public async Task<IActionResult> GetRegister(string contestId, CancellationToken token)
        {
            var contest = await DB.Contests.SingleOrDefaultAsync(x => x.Id == contestId, token);
            if (contest == null)
            {
                return Result(404, "The contest is not found");
            }

            var register = await DB.Attendees
                .Where(x => x.ContestId == contestId)
                .Where(x => x.UserId == User.Current.Id)
                .SingleOrDefaultAsync(token);

            if (register == null)
            {
                return Result(new
                {
                    isRegistered = false
                });
            }
            else
            {
                return Result(new
                {
                    isRegistered = true,
                    isVirtual = register.IsVirtual,
                    time = register.RegisterTime
                });
            }
        }

        [HttpPut("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/register")]
        public async Task<IActionResult> PutRegister(string contestId, CancellationToken token)
        {
            var contest = await DB.Contests.SingleOrDefaultAsync(x => x.Id == contestId, token);
            if (contest == null)
            {
                return Result(404, "The contest is not found");
            }

            var request = JsonConvert.DeserializeObject<ContestRegisterRequest>(RequestBody);

            if (contest.DisableVirtual && request.isVirtual)
            {
                return Result(400, "This contest does not accept a virtual competitor");
            }

            if (contest.AttendPermission == AttendPermission.Password && request.password != contest.PasswordOrTeamId)
            {
                return Result(400, "This password is incorrect");
            }

            if (contest.AttendPermission == AttendPermission.Team && !(await DB.GroupMembers.AnyAsync(x => x.UserId == User.Current.Id && x.GroupId == contest.PasswordOrTeamId)))
            {
                return Result(400, $"You are not a member of team '{ contest.PasswordOrTeamId }'");
            }

            if (contest.End <= DateTime.Now && !request.isVirtual)
            {
                return Result(400, "The contest is end");
            }

            var register = await DB.Attendees
                .Where(x => x.ContestId == contestId)
                .Where(x => x.UserId == User.Current.Id)
                .SingleOrDefaultAsync(token);

            if (register != null)
            {
                return Result(400, "You have already registered this contest.");
            }

            register = new Attendee()
            {
                ContestId = contestId,
                IsVirtual = request.isVirtual,
                RegisterTime = DateTime.Now,
                UserId = User.Current.Id
            };
            DB.Attendees.Add(register);
            await DB.SaveChangesAsync(token);

            return Result(200, "Register succeeded");
        }
        #endregion

        #region Claims
        [HttpGet("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/all")]
        public async Task<IActionResult> GetClaims(string contestId, CancellationToken token)
        {
            var ret = await DB.UserClaims
                .Where(x => x.ClaimType == Constants.ContestEditPermission)
                .Where(x => x.ClaimValue == contestId)
                .ToListAsync(token);
            return Result(ret);
        }

        [HttpPut("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim")]
        public async Task<IActionResult> PutClaims(string contestId, [FromBody] IdentityUserClaim<Guid> value, CancellationToken token)
        {
            if (!await HasPermissionToContestAsync(contestId, token))
            {
                return Result(401, "No permission");
            }
            else if (await DB.UserClaims.AnyAsync(x => x.ClaimValue == contestId && x.ClaimType == Constants.ContestEditPermission && x.UserId == value.UserId, token))
            {
                return Result(400, "Already exists");
            }
            else
            {
                DB.UserClaims.Add(new IdentityUserClaim<Guid>
                {
                    ClaimType = Constants.ContestEditPermission,
                    UserId = value.UserId,
                    ClaimValue = contestId
                });
                await DB.SaveChangesAsync(token);
                return Result(200, "Succeeded");
            }
        }

        [HttpPut("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/{userId:Guid}")]
        public async Task<IActionResult> DeleteClaim(Guid userId, string contestId, CancellationToken token)
        {
            if (!await HasPermissionToContestAsync(contestId, token))
            {
                return Result(401, "No permission");
            }
            else if (!await DB.UserClaims.AnyAsync(x => x.ClaimValue == contestId && x.ClaimType == Constants.ContestEditPermission && x.UserId == userId, token))
            {
                return Result(404, "Claim not found");
            }
            else if (userId == User.Current.Id)
            {
                return Result(400, "Cannot remove yourself");
            }
            else
            {
                await DB.UserClaims
                    .Where(x => x.ClaimValue == contestId && x.ClaimType == Constants.ContestEditPermission && x.UserId == userId)
                    .DeleteAsync(token);

                return Result(200, "Delete succeeded");
            }
        }
        #endregion

        #region Standings
        [HttpGet("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/standings/all")]
        public async Task<IActionResult> GetStandings(string contestId, bool? includingVirtual, CancellationToken token)
        {
            // TODO: Hide OI standings
            if (!includingVirtual.HasValue)
                includingVirtual = true;
            var exceptNestedQuery = DB.Attendees
                .Where(x => x.ContestId == contestId)
                .Where(x => x.IsVirtual)
                .Select(x => x.UserId);
            IQueryable<ContestProblemLastStatus> query = DB.ContestProblemLastStatuses
                .Where(x => x.ContestId == contestId);
            if (!includingVirtual.Value)
                query = query.Where(x => !exceptNestedQuery.Contains(x.UserId));
            dynamic ret = await query
                .GroupBy(x => x.UserId)
                .Select(x => new Standings
                {
                    UserId = x.Key,
                    Point = x.Count() > 0 ? x.Sum(y => y.Point) : 0,
                    Point2 = x.Count() > 0 ? x.Sum(y => y.Point2) : 0,
                    Point3 = x.Count() > 0 ? x.Sum(y => y.Point3) : 0,
                    TimeSpan = x.Count() > 0 ? new TimeSpan(x.Sum(y => y.TimeSpan.Ticks)) : new TimeSpan(),
                    TimeSpan2 = x.Count() > 0 ? new TimeSpan(x.Sum(y => y.TimeSpan2.Ticks)) : new TimeSpan(),
                    Statuses = x.ToList()
                })
                .OrderByDescending(x => x.Point)
                .ThenByDescending(x => x.Point2)
                .ThenBy(x => x.Point3)
                .ThenBy(x => x.TimeSpan)
                .ThenBy(x => x.TimeSpan2)
                .ToListAsync(token);

            return Result(ret);
        }

        [HttpGet("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/standings/{userId:Guid}")]
        public async Task<IActionResult> GetStandings(string contestId, Guid userId, CancellationToken token)
        {
            // TODO: Hide OI standings
            var statuses = await DB.ContestProblemLastStatuses
                .Where(x => x.ContestId == contestId)
                .Where(x => x.UserId == userId)
                .ToListAsync(token);

            var ret = new Standings
            {
                UserId = userId,
                Point = statuses.Count > 0 ? statuses.Sum(x => x.Point) : 0,
                Point2 = statuses.Count > 0 ? statuses.Sum(x => x.Point2) : 0,
                Point3 = statuses.Count > 0 ? statuses.Sum(x => x.Point3) : 0,
                TimeSpan = statuses.Count > 0 ? new TimeSpan(statuses.Sum(x => x.TimeSpan.Ticks)) : new TimeSpan(),
                TimeSpan2 = statuses.Count > 0 ? new TimeSpan(statuses.Sum(x => x.TimeSpan2.Ticks)) : new TimeSpan(),
                Statuses = statuses.ToList()
            };

            return Result(ret);
        }
        #endregion

        #region Lock
        [HttpGet("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/lock")]
        public async Task<IActionResult> GetAllLocks(string contestId, CancellationToken token)
        {
            if (User.Current == null)
            {
                return Result<IEnumerable<string>>(401, "Not authorized");
            }
            else
            {
                var contest = await DB.Contests
                    .SingleOrDefaultAsync(x => x.Id == contestId, token);
                if (contest == null)
                {
                    return Result<IEnumerable<string>>(404, "Contest not found");
                }
                else if (contest.Type != ContestType.Codeforces)
                {
                    return Result<IEnumerable<string>>(400, "You can only get the lock status in Codeforces contest");
                }
                else
                {
                    var ret = await DB.ContestProblemLastStatuses
                        .Where(x => x.UserId == User.Current.Id)
                        .Where(x => x.IsLocked)
                        .Select(x => x.ProblemId)
                        .ToListAsync(token);

                    return Result(ret.AsEnumerable());
                }
            }
        }

        [HttpPut("{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/lock/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> PutLock(string contestId, string problemId, CancellationToken token)
        {
            if (User.Current == null)
            {
                return Result(401, "Not authorized");
            }
            else
            {
                var status = await DB.ContestProblemLastStatuses
                    .Where(x => x.ContestId == contestId)
                    .Where(x => x.UserId == User.Current.Id)
                    .Where(x => x.ProblemId == problemId)
                    .SingleOrDefaultAsync(token);

                if (status == null)
                {
                    return Result(404, "Not found");
                }
                else if (status.IsLocked)
                {
                    return Result(400, "Already locked");
                }
                else
                {
                    status.IsLocked = true;
                    await DB.SaveChangesAsync(token);
                    return Result(200, "Put succeeded");
                }
            }
        }
        #endregion

        #region Private Functions
        private const string ProblemNumberString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

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
