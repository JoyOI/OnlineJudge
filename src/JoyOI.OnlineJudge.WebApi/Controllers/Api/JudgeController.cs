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
using JoyOI.ManagementService.Model.Dtos;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.OnlineJudge.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class JudgeController : BaseController
    {
        [HttpGet("all")]
        public async Task<IActionResult> Get(string problemId, JudgeResult? status, Guid? userId, string contestId, string language, int? page, DateTime? begin, DateTime? end, CancellationToken token)
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

            if (begin.HasValue)
            {
                ret = ret.Where(x => begin.Value <= x.CreatedTime);
            }

            if (end.HasValue)
            {
                ret = ret.Where(x => x.CreatedTime <= end.Value);
            }

            var result = await DoPaging(ret.OrderByDescending(x => x.CreatedTime), page ?? 1, 50, token);
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
                x.User = new User { Id = x.UserId, UserName = users[x.UserId] };
            }

            return Json(result);
        }

        [HttpPut]
        public async Task<IActionResult> Put(
            [FromServices] IConfiguration Config,
            [FromServices] IServiceScopeFactory scopeFactory,
            [FromServices] StateMachineAwaiter awaiter,
            [FromServices] ManagementServiceClient MgmtSvc,
            [FromServices] IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            var request = JsonConvert.DeserializeObject<JudgeRequest>(RequestBody);
            var problem = await DB.Problems
                .Include(x => x.TestCases)
                .SingleOrDefaultAsync(x => x.Id == request.problemId, token);

            if (problem == null)
            {
                return Result<Guid>(400, "The problem does not exist.");
            }

            if (!problem.IsVisiable && string.IsNullOrWhiteSpace(request.contestId) && !await HasPermissionToProblemAsync(problem.Id, token))
            {
                return Result<Guid>(403, "You have no permission to the problem.");
            }

            // TODO: Check contest permission

            if (!Constants.CompileNeededLanguages.Contains(request.language) && !Constants.ScriptLanguages.Contains(request.language))
            {
                return Result<Guid>(400, "The programming language which you selected was not supported");
            }

            if (request.isSelfTest && request.data.Count() == 0)
            {
                return Result<Guid>(400, "The self testing data has not been found");
            }

            var blobs = new ConcurrentDictionary<int, BlobInfo[]>();
            blobs.TryAdd(-1, new[] { new BlobInfo
                {
                    Id = await MgmtSvc.PutBlobAsync("Main" + Constants.GetExtension(request.language), Encoding.UTF8.GetBytes(request.code)),
                    Name = "Main" + Constants.GetExtension(request.language),
                    Tag = "Problem=" + problem.Id
                }
            });
            blobs.TryAdd(-2, new[] { new BlobInfo
                {
                    Id = await MgmtSvc.PutBlobAsync("limit.json", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        UserTime = problem.TimeLimitationPerCaseInMs,
                        PhysicalTime = problem.TimeLimitationPerCaseInMs * 2,
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
                        Name = "Validator.out"
                    }
                });
            }
            else
            {
                // TODO: Special Judge
            }

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
                // TODO: Test case type
                var testCases = await DB.TestCases
                    .Where(x => x.ProblemId == problem.Id && (x.Type == TestCaseType.Small || x.Type == TestCaseType.Large))
                    .ToListAsync(token);
                for (var i = 0; i < testCases.Count; i++)
                {
                    blobs.TryAdd(i, new[] {
                        new BlobInfo { Id = testCases[i].InputBlobId, Name = $"input_{ i }.txt", Tag = i.ToString() },
                        new BlobInfo { Id = testCases[i].OutputBlobId, Name = $"output_{ i }.txt", Tag = i.ToString() }
                    });
                }
            }

            var stateMachineId = await MgmtSvc.PutStateMachineInstanceAsync("JudgeStateMachine", null, blobs.SelectMany(x => x.Value), token);

            var substatuses = blobs
                .Where(x => x.Key >= 0)
                .Select(x => new SubJudgeStatus
                {
                    SubId = x.Key,
                    Result = JudgeResult.Pending,
                    InputBlobId = x.Value.Single(y => y.Name.StartsWith("input_")).Id,
                    OutputBlobId = x.Value.Single(y => y.Name.StartsWith("output_")).Id,
                })
                .ToList();

            var status = new JudgeStatus
            {
                Code = request.code,
                Language = request.language,
                Result = JudgeResult.Pending,
                CreatedTime = DateTime.Now,
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
                                CreatedTime = DateTime.Now,
                                Name = "JudgeStateMachine",
                                Id = stateMachineId
                            },
                            StateMachineId = stateMachineId
                        }
                    },
            };

            DB.JudgeStatuses.Add(status);
            await DB.SaveChangesAsync(token);
            Task.Factory.StartNew(async () =>
            {
                using (var scope = scopeFactory.CreateScope())
                using (var db = scope.ServiceProvider.GetService<OnlineJudgeContext>())
                {
                    try
                    {
                        var result = await awaiter.GetStateMachineResultAsync(stateMachineId, default(CancellationToken));
                        await HandleJudgeResultAsync(db, MgmtSvc, status.Id, result, problem, status.UserId, request.isSelfTest, hub,  default(CancellationToken));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }
            });

            return Result(status.Id);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken token)
        {
            var ret = await DB.JudgeStatuses
                .Include(x => x.SubStatuses)
                .Include(x => x.Problem)
                .SingleOrDefaultAsync(x => x.Id == id, token);

            var problem = ret.Problem;
            ret.Problem = null;
            ret.User = null;

            var hasPermissionToProblem = await HasPermissionToProblemAsync(problem.Id, token);

            if (!problem.IsVisiable && hasPermissionToProblem)
            {
                return Result<JudgeStatus>(403, "No permission");
            }

            if (!string.IsNullOrWhiteSpace(ret.ContestId))
            {
                var contest = DB.Contests
                    .Single(x => x.Id == ret.ContestId);

                // TODO: Handle contest status display
            }

            if (User.Current != null && User.Current.Id == ret.UserId)
            {
                HasOwnership = true;
            }

            if (!HasOwnership && !hasPermissionToProblem && !await HasPermissionToContestAsync(ret.ContestId, token))
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
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == problemId));

        private async Task<string> ReadBlobAsStringAsync(ManagementServiceClient mgmt, Guid blobId, CancellationToken token)
        {
            var blob = await mgmt.GetBlobAsync(blobId, token);
            return Encoding.UTF8.GetString(blob.Body);
        }

        private async Task<T> ReadBlobAsObjectAsync<T>(ManagementServiceClient mgmt, Guid blobId, CancellationToken token)
        {
            var jsonString = await ReadBlobAsStringAsync(mgmt, blobId, token);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        private int GetStateMachineTestCaseCount(StateMachineInstanceOutputDto statemachine)
        {
            return statemachine.InitialBlobs.Where(x => x.Name.StartsWith("input_")).Count();
        }

        private async Task<(bool result, string hint)> IsFailedInCompileStageAsync(ManagementServiceClient mgmt, StateMachineInstanceOutputDto statemachine, CancellationToken token)
        {
            if (statemachine.StartedActors.Count() == 1 && statemachine.StartedActors.Last().Name == "CompileActor")
            {
                var runner = await ReadBlobAsObjectAsync<Runner>(mgmt, statemachine.StartedActors.Last().Outputs.Single(x => x.Name == "runner.json").Id, token);
                var stdout = await ReadBlobAsStringAsync(mgmt, statemachine.StartedActors.Last().Outputs.Single(x => x.Name == "stdout.txt").Id, token);
                var stderr = await ReadBlobAsStringAsync(mgmt, statemachine.StartedActors.Last().Outputs.Single(x => x.Name == "stderr.txt").Id, token);
                return (true, string.Join(Environment.NewLine, runner.Error, stdout, stderr));
            }
            else
            {
                return (false, statemachine.StartedActors.First(x => x.Name == "CompileActor").Outputs.Single(x => x.Name.StartsWith("Main")).Id.ToString());
            }
        }

        private async Task<IEnumerable<(int subId, JudgeResult result, int time, int memory, string hint)>> HandleRuntimeResultAsync(ManagementServiceClient mgmt, StateMachineInstanceOutputDto statemachine, int memoryLimit, CancellationToken token)
        {
            var runActors = statemachine.StartedActors
                .Where(x => x.Name == "RunUserProgramActor");

            var compareActors = statemachine.StartedActors
                .Where(x => x.Name == "CompareActor");

            var testCaseCount = GetStateMachineTestCaseCount(statemachine);

            if (runActors.Count() != testCaseCount)
            {
                throw new Exception("Missing RunUserProgramActor");
            }

            var ret = new List<(int subId, JudgeResult result, int time, int memory, string hint)>();
            for (var i = 0; i < testCaseCount; i++)
            {
                var actor = compareActors.SingleOrDefault(x => x.Tag == i.ToString());
                if (actor == null)
                {
                    actor = runActors.Single(x => x.Tag == i.ToString());
                    var runner = await ReadBlobAsObjectAsync<Runner>(mgmt, actor.Outputs.Single(x => x.Name == "runner.json").Id, token);
                    if (runner.IsTimeout)
                    {
                        ret.Add((
                            i,
                            JudgeResult.TimeExceeded,
                            runner.UserTime,
                            runner.PeakMemory,
                            string.Join(Environment.NewLine, actor.Exceptions) + Environment.NewLine + runner.Error));
                    }
                    else if (runner.ExitCode == 139 || actor.Exceptions.Any(x => x.Contains("May cause by out of memory")) || runner.Error.Contains("std::bad_alloc"))
                    {
                        ret.Add((
                            i,
                            JudgeResult.MemoryExceeded,
                            runner.UserTime,
                            memoryLimit,
                            string.Join(Environment.NewLine, actor.Exceptions) + Environment.NewLine + runner.Error));
                    }
                    else
                    {
                        ret.Add((
                            i,
                            JudgeResult.RuntimeError,
                            runner.UserTime,
                            runner.PeakMemory,
                            string.Join(Environment.NewLine, actor.Exceptions) + Environment.NewLine + runner.Error + Environment.NewLine + $"User process exited with code { runner.ExitCode }"));
                    }
                }
                else
                {
                    var runActor = runActors.Single(x => x.Tag == i.ToString());
                    var compareRunner = await ReadBlobAsObjectAsync<Runner>(mgmt, actor.Outputs.Single(x => x.Name == "runner.json").Id, token);
                    var runRunner = await ReadBlobAsObjectAsync<Runner>(mgmt, runActor.Outputs.Single(x => x.Name == "runner.json").Id, token);
                    if (compareRunner.ExitCode == 0)
                    {
                        ret.Add((
                            i,
                            JudgeResult.Accepted,
                            runRunner.UserTime,
                            runRunner.PeakMemory,
                            "Congratulations!"));
                    }
                    else if (compareRunner.ExitCode == 1)
                    {
                        var validatorStdout = await ReadBlobAsStringAsync(mgmt, actor.Outputs.Single(x => x.Name == "stdout.txt").Id, token);
                        ret.Add((
                            i,
                            JudgeResult.PresentationError,
                            runRunner.UserTime,
                            runRunner.PeakMemory,
                            validatorStdout));
                    }
                    else if (compareRunner.ExitCode == 2)
                    {
                        var validatorStdout = await ReadBlobAsStringAsync(mgmt, actor.Outputs.Single(x => x.Name == "stdout.txt").Id, token);
                        ret.Add((
                            i,
                            JudgeResult.WrongAnswer,
                            runRunner.UserTime,
                            runRunner.PeakMemory,
                            validatorStdout));
                    }
                    else
                    {
                        ret.Add((
                            i,
                            JudgeResult.SystemError,
                            runRunner.UserTime,
                            runRunner.PeakMemory,
                            string.Join(Environment.NewLine, actor.Exceptions) + Environment.NewLine + compareRunner.Error + Environment.NewLine + $"Validator process exited with code { compareRunner.ExitCode }"));
                    }
                }
            }

            return ret;
        }

        private async Task HandleJudgeResultAsync(
            OnlineJudgeContext db, 
            ManagementServiceClient mgmt, 
            Guid statusId, 
            StateMachineInstanceOutputDto statemachine, 
            Problem problem, 
            Guid userId,
            bool isSelfTest,
            IHubContext<OnlineJudgeHub> hub,
            CancellationToken token)
        {
            bool isAccepted = false;
            var compileResult = await IsFailedInCompileStageAsync(mgmt, statemachine, token);
            if (compileResult.result)
            {
                db.JudgeStatuses
                    .Where(x => x.Id == statusId)
                    .SetField(x => x.Result).WithValue((int)JudgeResult.CompileError)
                    .SetField(x => x.Hint).WithValue(compileResult.hint)
                    .Update();
                db.SubJudgeStatuses
                    .Where(x => x.StatusId == statusId)
                    .SetField(x => x.Result).WithValue((int)JudgeResult.CompileError)
                    .Update();
            }
            else
            {
                var runtimeResult = await HandleRuntimeResultAsync(mgmt, statemachine, problem.MemoryLimitationPerCaseInByte, token);
                var finalResult = runtimeResult.Max(x => x.result);
                var finalTime = runtimeResult.Sum(x => x.time);
                var finalMemory = runtimeResult.Max(x => x.memory);

                db.JudgeStatuses
                    .Where(x => x.Id == statusId)
                    .SetField(x => x.TimeUsedInMs).WithValue(finalTime)
                    .SetField(x => x.MemoryUsedInByte).WithValue(finalMemory)
                    .SetField(x => x.Result).WithValue((int)finalResult)
                    .Update();

                for (var i = 0; i < runtimeResult.Count(); i++)
                {
                    db.SubJudgeStatuses
                        .Where(x => x.StatusId == statusId)
                        .Where(x => x.SubId == i)
                        .SetField(x => x.TimeUsedInMs).WithValue(runtimeResult.ElementAt(i).time)
                        .SetField(x => x.MemoryUsedInByte).WithValue(runtimeResult.ElementAt(i).memory)
                        .SetField(x => x.Result).WithValue((int)runtimeResult.ElementAt(i).result)
                        .SetField(x => x.Hint).WithValue(runtimeResult.ElementAt(i).hint)
                        .Update();
                }

                if (finalResult == JudgeResult.Accepted)
                {
                    isAccepted = true;
                }
            }

            if (!isSelfTest)
            {
                UpdateUserProblemJson(db, userId, problem.Id, isAccepted);
            }

            hub.Clients.All.InvokeAsync("ItemUpdated", "judge", statusId);
        }

        private void UpdateUserProblemJson(OnlineJudgeContext db, Guid userId, string problemId, bool isAccepted)
        {
            var effectedRows = 0;
            while (effectedRows == 0)
            {
                var user = db.Users.AsNoTracking().Single(x => x.Id == userId);
                var timestamp = user.ConcurrencyStamp;
                if (!user.TriedProblems.Object.Contains(problemId))
                {
                    user.TriedProblems.Object.Add(problemId);
                    effectedRows = db.Users
                        .Where(x => x.Id == userId)
                        .Where(x => x.ConcurrencyStamp == timestamp)
                        .SetField(x => x.TriedProblems).WithValue(JsonConvert.SerializeObject(user.TriedProblems.Object))
                        .SetField(x => x.ConcurrencyStamp).WithValue(Guid.NewGuid())
                        .Update();
                }
                else
                {
                    break;
                }
            }

            if (isAccepted)
            {
                effectedRows = 0;
                while (effectedRows == 0)
                {
                    var user = db.Users.AsNoTracking().Single(x => x.Id == userId);
                    var timestamp = user.ConcurrencyStamp;
                    if (!user.PassedProblems.Object.Contains(problemId))
                    {
                        user.PassedProblems.Object.Add(problemId);
                        effectedRows = db.Users
                            .Where(x => x.Id == userId)
                            .Where(x => x.ConcurrencyStamp == timestamp)
                            .SetField(x => x.PassedProblems).WithValue(JsonConvert.SerializeObject(user.PassedProblems.Object))
                            .SetField(x => x.ConcurrencyStamp).WithValue(Guid.NewGuid())
                            .Update();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        #endregion
    }
}
