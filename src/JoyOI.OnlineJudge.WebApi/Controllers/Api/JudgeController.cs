using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using JoyOI.ManagementService.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.OnlineJudge.WebApi.Hubs;
using JoyOI.OnlineJudge.ContestExecutor;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class JudgeController : BaseController
    {
        [HttpGet("all")]
        public async Task<IActionResult> Get(
            string problemId, 
            JudgeResult? status, 
            string userId, 
            string contestId, 
            string language, 
            int? page, 
            DateTime? begin, 
            DateTime? end,
            string judgeIds,
            [FromServices] ContestExecutorFactory cef,
            CancellationToken token)
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

            if (!string.IsNullOrWhiteSpace(userId))
            {
                ret = ret.Where(x => x.User.UserName == userId);
            }

            if (!string.IsNullOrWhiteSpace(contestId))
            {
                ret = ret.Where(x => x.ContestId == contestId);
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                ret = ret.Where(x => x.Language == language);
            }

            if (begin.HasValue)
            {
                ret = ret.Where(x => begin.Value <= x.CreatedTime);
            }

            if (end.HasValue)
            {
                ret = ret.Where(x => x.CreatedTime <= end.Value);
            }

            if (!string.IsNullOrWhiteSpace(judgeIds))
            {
                var ids = judgeIds.Split(',').Select(x => Guid.Parse(x.Trim()));
                ret = ret.Where(x => ids.Contains(x.Id));
            }

            if (IsGroupRequest())
            {
                ret = ret.Where(x => x.GroupId == CurrentGroup.Id);
            }

            var result = await DoPaging(ret.OrderByDescending(x => x.CreatedTime), page ?? 1, 20, token);
            if (!IsMasterOrHigher && result.data.result.Any(x => !string.IsNullOrWhiteSpace(x.ContestId)))
            {
                var pendingRemove = new ConcurrentBag<JudgeStatus>();
                foreach (var x in result.data.result.Where(x => !string.IsNullOrWhiteSpace(x.ContestId)))
                {
                    var ce = cef.Create(x.ContestId);
                    var submittorUsername = DB.Users.Single(y => y.Id == x.UserId).UserName;
                    var isContestInProgress = ce.IsContestInProgress(User.Current?.UserName) || ce.IsContestInProgress(submittorUsername);
                    if (isContestInProgress && !await HasPermissionToContestAsync(x.ContestId, token) && !await HasPermissionToProblemAsync(x.ProblemId, token))
                    {
                        if (ce.AllowFilterByJudgeResult || !status.HasValue)
                        {
                            ce.OnShowJudgeResult(x);
                        }
                        else
                        {
                            pendingRemove.Add(x);
                        }
                    }
                    x.Contest = null;
                }
                foreach (var x in pendingRemove)
                {
                    (result.data.result as List<JudgeStatus>).Remove(x);
                }
            }

            FilterResult(result.data.result);
            return Json(result);
        }

        [HttpPut]
        public async Task<IActionResult> Put(
            [FromServices] IConfiguration Config,
            [FromServices] IServiceScopeFactory scopeFactory,
            [FromServices] StateMachineAwaiter awaiter,
            [FromServices] ManagementServiceClient MgmtSvc,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            [FromServices] ContestExecutorFactory cef,
            CancellationToken token)
        {
            var request = JsonConvert.DeserializeObject<JudgeRequest>(RequestBody);
            var problem = await DB.Problems
                .Include(x => x.TestCases)
                .SingleOrDefaultAsync(x => x.Id == request.problemId, token);

            if (problem == null)
            {
                return Result(400, "The problem does not exist.");
            }

            if (!problem.IsVisible && string.IsNullOrWhiteSpace(request.contestId) && !await HasPermissionToProblemAsync(problem.Id, token))
            {
                return Result(403, "You have no permission to the problem.");
            }

            if (!Constants.SupportedLanguages.Contains(request.language))
            {
                return Result(400, "The language has not been supported.");
            }

            if (!string.IsNullOrEmpty(request.contestId))
            {
                var ce = cef.Create(request.contestId);

                if (!ce.IsContestInProgress(User.Current?.UserName))
                {
                    return Result(400, "The contest is inactive.");
                }

                if (request.isSelfTest)
                {
                    return Result(400, "You could not do a self test during the contest is in progress.");
                }

                if (await DB.ContestProblemLastStatuses.AnyAsync(x => x.IsLocked && x.ContestId == request.contestId && User.Current.Id == x.UserId && x.ProblemId == request.problemId, token))
                {
                    return Result(400, "You have locked this problem.");
                }
            }

            if (!Constants.SupportedLanguages.Contains(request.language) && !Constants.UnsupportedLanguages.Contains(request.language) && problem.Source == ProblemSource.Local)
            {
                return Result(400, "The programming language which you selected was not supported");
            }

            if (request.isSelfTest && request.data.Count() == 0)
            {
                return Result(400, "The self testing data has not been found");
            }

            if (problem.Source != ProblemSource.Local && request.isSelfTest)
            {
                return Result(400, "You could not use self data to test with a remote problem.");
            }

            if (IsGroupRequest() && !await DB.GroupProblems.AnyAsync(x => x.ProblemId == problem.Id && x.GroupId == CurrentGroup.Id) && string.IsNullOrEmpty(request.contestId))
            {
                return Result(404, "The problem does not exist.");
            }

            #region Local Judge
            if (problem.Source == ProblemSource.Local)
            {
                var blobs = new ConcurrentDictionary<int, BlobInfo[]>();
                blobs.TryAdd(-1, new[] { new BlobInfo
                {
                    Id = await MgmtSvc.PutBlobAsync("Main" + Constants.GetSourceExtension(request.language), Encoding.UTF8.GetBytes(request.code)),
                    Name = "Main" + Constants.GetSourceExtension(request.language),
                    Tag = "Problem=" + problem.Id
                }
            });
                blobs.TryAdd(-2, new[] { new BlobInfo
                {
                    Id = await MgmtSvc.PutBlobAsync("limit.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        UserTime = problem.TimeLimitationPerCaseInMs,
                        PhysicalTime = problem.TimeLimitationPerCaseInMs * 4,
                        Memory = problem.MemoryLimitationPerCaseInByte
                    }))),
                    Name = "limit.json",
                    Tag = "Problem=" +  problem.Id
                }
            });
                if (!problem.ValidatorBlobId.HasValue)
                {
                    blobs.TryAdd(-3, new[]
                    {
                    new BlobInfo
                    {
                        Id = Guid.Parse(Config["JoyOI:StandardValidatorBlobId"]),
                        Name = "Validator" + Constants.GetBinaryExtension(problem.ValidatorLanguage)
                    }
                });
                }
                else
                {
                    // TODO: Special Judge
                }

                List<TestCase> testCases = null;
                if (request.isSelfTest)
                {
                    Parallel.For(0, request.data.Count(), i =>
                    {
                        // Uploading custom data
                        var inputId = MgmtSvc.PutBlobAsync($"input_{ i }.txt", Encoding.UTF8.GetBytes(request.data.ElementAt(i).input), token).Result;
                        var outputId = MgmtSvc.PutBlobAsync($"output_{ i }.txt", Encoding.UTF8.GetBytes(request.data.ElementAt(i).output), token).Result;
                        blobs.TryAdd(i, new[] {
                        new BlobInfo { Id = inputId, Name = $"input_{ i }.txt", Tag = i.ToString() },
                        new BlobInfo { Id = outputId, Name = $"output_{ i }.txt", Tag =  i.ToString() }
                    });
                    });
                }
                else
                {
                    testCases = await DB.TestCases
                        .Where(x => x.ProblemId == problem.Id && (x.Type == TestCaseType.Small || x.Type == TestCaseType.Large || x.Type == TestCaseType.Hack))
                        .ToListAsync(token);

                    if (testCases.Count == 0)
                    {
                        return Result(400, "No test case found.");
                    }

                    for (var i = 0; i < testCases.Count; i++)
                    {
                        blobs.TryAdd(i, new[] {
                        new BlobInfo { Id = testCases[i].InputBlobId, Name = $"input_{ i }.txt", Tag = i.ToString() },
                        new BlobInfo { Id = testCases[i].OutputBlobId, Name = $"output_{ i }.txt", Tag = i.ToString() }
                    });
                    }
                }

                var stateMachineId = await MgmtSvc.PutStateMachineInstanceAsync("JudgeStateMachine", Config["ManagementService:CallBack"], blobs.SelectMany(x => x.Value), await CalculatePriorityAsync(), token);

                var substatuses = blobs
                    .Where(x => x.Key >= 0)
                    .Select(x => new SubJudgeStatus
                    {
                        SubId = x.Key,
                        Result = JudgeResult.Pending,
                        InputBlobId = x.Value.Single(y => y.Name.StartsWith("input_")).Id,
                        OutputBlobId = x.Value.Single(y => y.Name.StartsWith("output_")).Id,
                        TestCaseId = testCases != null ? (Guid?)testCases[x.Key].Id : null
                    })
                    .ToList();

                var status = new JudgeStatus
                {
                    Code = request.code,
                    Language = request.language,
                    Result = JudgeResult.Pending,
                    CreatedTime = DateTime.UtcNow,
                    ContestId = request.contestId,
                    SubStatuses = substatuses,
                    ProblemId = problem.Id,
                    UserId = User.Current.Id,
                    IsSelfTest = request.isSelfTest,
                    RelatedStateMachineIds = new List<JudgeStatusStateMachine>
                    {
                        new JudgeStatusStateMachine
                        {
                            StateMachine = new StateMachine
                            {
                                CreatedTime = DateTime.UtcNow,
                                Name = "JudgeStateMachine",
                                Id = stateMachineId
                            },
                            StateMachineId = stateMachineId
                        }
                    },
                };

                if (IsGroupRequest())
                {
                    status.GroupId = CurrentGroup.Id;
                }

                DB.JudgeStatuses.Add(status);
                await DB.SaveChangesAsync(token);

                hub.Clients.All.InvokeAsync("ItemUpdated", "judge", status.Id);

                // For debugging
                if (Config["ManagementService:Mode"] == "Polling")
                {
                    Task.Factory.StartNew(async () =>
                    {
                        using (var scope = scopeFactory.CreateScope())
                        using (var db = scope.ServiceProvider.GetService<OnlineJudgeContext>())
                        {
                            try
                            {
                                await awaiter.GetStateMachineResultAsync(stateMachineId);
                                var handler = scope.ServiceProvider.GetService<JudgeStateMachineHandler>();
                                await handler.HandleJudgeResultAsync(stateMachineId, default(CancellationToken));
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(ex);
                            }
                        }
                    });
                }
                return Result(status.Id);
            }
            #endregion
            #region Bzoj, LeetCode, CodeVS
            else if (problem.Source == ProblemSource.Bzoj || problem.Source == ProblemSource.LeetCode || problem.Source == ProblemSource.CodeVS)
            {
                var metadata = new
                {
                    Source = problem.Source.ToString(),
                    Language = request.language,
                    Code = request.code,
                    ProblemId = problem.Id.Replace(problem.Source.ToString().ToLower() + "-", "")
                };

                var metadataBlob = new BlobInfo
                {
                    Id = await MgmtSvc.PutBlobAsync("metadata.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metadata)), token),
                    Name = "metadata.json",
                    Tag = "Problem=" + problem.Id
                };

                var stateMachineId = await MgmtSvc.PutStateMachineInstanceAsync("VirtualJudgeStateMachine", Config["ManagementService:Callback"], new[] { metadataBlob }, await CalculatePriorityAsync(), token);

                var status = new JudgeStatus
                {
                    Code = request.code,
                    ContestId = request.contestId,
                    Language = request.language,
                    ProblemId = problem.Id,
                    IsSelfTest = false,
                    UserId = User.Current.Id,
                    Result = JudgeResult.Pending,
                    RelatedStateMachineIds = new List<JudgeStatusStateMachine>
                    {
                        new JudgeStatusStateMachine {
                            StateMachine = new StateMachine
                            {
                                CreatedTime = DateTime.UtcNow,
                                Name = "JudgeStateMachine",
                                Id = stateMachineId
                            },
                            StateMachineId = stateMachineId
                        }
                    }
                };
                
                if (IsGroupRequest())
                {
                    status.GroupId = CurrentGroup.Id;
                }

                DB.JudgeStatuses.Add(status);
                await DB.SaveChangesAsync(token);

                // For debugging
                if (Config["ManagementService:Mode"] == "Polling")
                {
                    Task.Factory.StartNew(async () =>
                    {
                        using (var scope = scopeFactory.CreateScope())
                        {
                            try
                            {
                                await awaiter.GetStateMachineResultAsync(stateMachineId);
                                var handler = scope.ServiceProvider.GetService<JudgeStateMachineHandler>();
                                await handler.HandleJudgeResultAsync(stateMachineId, default(CancellationToken));
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(ex);
                            }
                        }
                    });
                }

                hub.Clients.All.InvokeAsync("ItemUpdated", "judge", status.Id);

                return Result(status.Id);
            }
            #endregion
            #region Others
            else
            {
                throw new NotSupportedException(problem.Source.ToString() + " has not been supported yet.");
            }
            #endregion
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id, [FromServices] ContestExecutorFactory cef, CancellationToken token)
        {
            var ret = await DB.JudgeStatuses
                .Include(x => x.User)
                .Include(x => x.SubStatuses)
                .Include(x => x.Problem)
                .SingleOrDefaultAsync(x => x.Id == id, token);
            
            if (IsGroupRequest() && ret.GroupId != CurrentGroup.Id)
            {
                return Result(400, "No permission");
            }

            var problem = ret.Problem;
            var username = ret.User.UserName;

            if (!string.IsNullOrWhiteSpace(ret.ContestId))
            {
                var contest = DB.Contests
                    .Single(x => x.Id == ret.ContestId);
                if (!await HasPermissionToContestAsync(ret.ContestId) && !await HasPermissionToProblemAsync(ret.ProblemId))
                {
                    var ce = cef.Create(contest.Id);
                    if (ce.IsContestInProgress(User.Current?.UserName) || ce.IsContestInProgress(username))
                    {
                        ce.OnShowJudgeResult(ret);
                    }
                }
            }
            else
            {
                var hasPermissionToProblem = await HasPermissionToProblemAsync(problem.Id, token);

                if (!problem.IsVisible && !hasPermissionToProblem && !IsGroupRequest() && !await HasPermissionToGroupAsync(token))
                {
                    return Result<JudgeStatus>(403, "No permission");
                }
            }

            if (User.Current != null && User.Current.Id == ret.UserId)
            {
                HasOwnership = true;
            }

            ret.IsHackable = await IsStatusCouldBeHacked(ret, cef, token);

            if (!HasOwnership
                && !await HasPermissionToProblemAsync(problem.Id, token)
                && !await HasPermissionToContestAsync(ret.ContestId, token)
                && !await IsStatusCodeCouldBeViewed(ret, cef, token)
                && !(IsGroupRequest() && await HasPermissionToGroupAsync(token)))
            {
                ret.Code = null;
            }

            ret.SubStatuses = ret.SubStatuses.OrderBy(x => x.SubId).ToList();

            return Result(ret);
        }

        #region Private Functions
        private async Task<bool> HasPermissionToContestAsync(string contestId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ContestEditPermission
                   && x.ClaimValue == contestId));

        private async Task<bool> HasPermissionToProblemAsync(string problemId, CancellationToken token = default(CancellationToken))
            => User.Current != null && (await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
            || await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == problemId));

        private async Task<int> CalculatePriorityAsync()
        {
            if (IsMasterOrHigher)
            {
                return 0;
            }
            else
            {
                const int basePRI = 1;

                var count15 = await DB.JudgeStatuses
                    .Where(x => x.UserId == User.Current.Id && x.CreatedTime >= DateTime.UtcNow.AddMinutes(-15))
                    .CountAsync() + await DB.HackStatuses
                    .Where(x => x.UserId == User.Current.Id && x.Time >= DateTime.UtcNow.AddMinutes(-15))
                    .CountAsync();

                var count60 = await DB.JudgeStatuses
                    .Where(x => x.UserId == User.Current.Id && x.CreatedTime >= DateTime.UtcNow.AddHours(-1))
                    .CountAsync() + await DB.HackStatuses
                    .Where(x => x.UserId == User.Current.Id && x.Time >= DateTime.UtcNow.AddHours(-1))
                    .CountAsync();

                return basePRI + Math.Max(count15 / 10, count60 / 30);
            }
        }

        private async Task<bool> IsContestAttendee(string contestId, CancellationToken token)
        {
            if (!User.IsSignedIn())
                return false;

            if (string.IsNullOrEmpty(contestId))
                return false;

            return await DB.Attendees.AnyAsync(x => x.ContestId == contestId && x.UserId == User.Current.Id, token);
        }

        private async Task<bool> IsStatusCodeCouldBeViewed(JudgeStatus status, ContestExecutorFactory cef, CancellationToken token)
        {
            if (!User.IsSignedIn() || status.IsSelfTest)
                return false;

            if (!string.IsNullOrEmpty(status.ContestId))
            {
                var ce = cef.Create(status.ContestId);
                if (ce.IsContestInProgress(status.User.UserName) || ce.IsContestInProgress())
                {
                    return ce.IsStatusHackable(status);
                }
            }

            if (Constants.HackInvalidResults.Contains(status.Result))
            {
                return false;
            }

            return await DB.JudgeStatuses.AnyAsync(x => x.UserId == User.Current.Id && x.ProblemId == status.ProblemId && string.IsNullOrEmpty(x.ContestId) && x.Result == JudgeResult.Accepted, token);
        }

        private async Task<bool> IsStatusCouldBeHacked(JudgeStatus status, ContestExecutorFactory cef, CancellationToken token)
        {
            if (User.IsSignedIn() && User.Current.Id == status.UserId)
                return false;
            return await IsStatusCodeCouldBeViewed(status, cef, token);
        }
        #endregion
    }
}
