using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using JoyOI.OnlineJudge.Models;
using JoyOI.ManagementService.SDK;
using JoyOI.ManagementService.Model.Dtos;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.OnlineJudge.WebApi.Hubs;
using JoyOI.OnlineJudge.WebApi.Lib;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    public class JudgeStateMachineHandler
    {
        private OnlineJudgeContext _db { get; set; }

        private OnlineJudgeHub _hub { get; set; }

        private ManagementServiceClient _mgmt { get; set; }

        public JudgeStateMachineHandler(OnlineJudgeContext db, OnlineJudgeHub hub, ManagementServiceClient mgmt)
        {
            _db = db;
            _hub = hub;
            _mgmt = mgmt;
        }

        private int GetStateMachineTestCaseCount(StateMachineInstanceOutputDto statemachine)
        {
            return statemachine.InitialBlobs.Where(x => x.Name.StartsWith("input_")).Count();
        }

        private async Task<(bool result, string hint)> IsFailedInCompileStageAsync(StateMachineInstanceOutputDto statemachine, CancellationToken token)
        {
            if (statemachine.StartedActors.Count() == 1 && statemachine.StartedActors.Last().Name == "CompileActor")
            {
                var runner = await _mgmt.ReadBlobAsObjectAsync<Runner>(statemachine.StartedActors.Last().Outputs.Single(x => x.Name == "runner.json").Id, token);
                var stdout = await _mgmt.ReadBlobAsStringAsync(statemachine.StartedActors.Last().Outputs.Single(x => x.Name == "stdout.txt").Id, token);
                var stderr = await _mgmt.ReadBlobAsStringAsync(statemachine.StartedActors.Last().Outputs.Single(x => x.Name == "stderr.txt").Id, token);
                return (true, string.Join(Environment.NewLine, runner.Error, stdout, stderr));
            }
            else
            {
                return (false, statemachine.StartedActors.First(x => x.Name == "CompileActor").Outputs.Single(x => x.Name.StartsWith("Main")).Id.ToString());
            }
        }

        private async Task<IEnumerable<(int subId, JudgeResult result, int time, int memory, string hint)>> HandleRuntimeResultAsync(StateMachineInstanceOutputDto statemachine, int memoryLimit, CancellationToken token)
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
                    var runner = await _mgmt.ReadBlobAsObjectAsync<Runner>(actor.Outputs.Single(x => x.Name == "runner.json").Id, token);
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
                    var compareRunner = await _mgmt.ReadBlobAsObjectAsync<Runner>(actor.Outputs.Single(x => x.Name == "runner.json").Id, token);
                    var runRunner = await _mgmt.ReadBlobAsObjectAsync<Runner>(runActor.Outputs.Single(x => x.Name == "runner.json").Id, token);
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
                        var validatorStdout = await _mgmt.ReadBlobAsStringAsync(actor.Outputs.Single(x => x.Name == "stdout.txt").Id, token);
                        ret.Add((
                            i,
                            JudgeResult.PresentationError,
                            runRunner.UserTime,
                            runRunner.PeakMemory,
                            validatorStdout));
                    }
                    else if (compareRunner.ExitCode == 2)
                    {
                        var validatorStdout = await _mgmt.ReadBlobAsStringAsync(actor.Outputs.Single(x => x.Name == "stdout.txt").Id, token);
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

        public async Task HandleJudgeResultAsync(
            Guid statemachineId,
            CancellationToken token)
        {
            bool isAccepted = false;
            var statemachine = await _mgmt.GetStateMachineInstanceAsync(statemachineId, token);

            if (statemachine.Status == ManagementService.Model.Enums.StateMachineStatus.Running)
            {
                throw new InvalidOperationException("The statemachine status is: " + statemachine.Status.ToString());
            }

            var statusStatemachineRelation =(await _db.JudgeStatusStateMachines.FirstOrDefaultAsync(x => x.StateMachineId == statemachineId, token));
            if (statusStatemachineRelation == null)
            {
                throw new KeyNotFoundException("Did not find the status which related to the statemachine " + statemachineId);
            }
            var statusId = statusStatemachineRelation.StatusId;
            var status = await _db.JudgeStatuses.Where(x => x.Id == statusId).Select(x => new { x.ProblemId, x.UserId, x.IsSelfTest }).FirstOrDefaultAsync(token);
            var userId = status.UserId;
            var problemId = status.ProblemId;
            var isSelfTest = status.IsSelfTest;
            var problem = await _db.Problems.SingleAsync(x => x.Id == problemId, token);
            var compileResult = await IsFailedInCompileStageAsync(statemachine, token);
            if (compileResult.result)
            {
                _db.JudgeStatuses
                    .Where(x => x.Id == statusId)
                    .SetField(x => x.Result).WithValue((int)JudgeResult.CompileError)
                    .SetField(x => x.Hint).WithValue(compileResult.hint)
                    .Update();
                _db.SubJudgeStatuses
                    .Where(x => x.StatusId == statusId)
                    .SetField(x => x.Result).WithValue((int)JudgeResult.CompileError)
                    .Update();
            }
            else
            {
                var runtimeResult = await HandleRuntimeResultAsync(statemachine, problem.MemoryLimitationPerCaseInByte, token);
                var finalResult = runtimeResult.Max(x => x.result);
                var finalTime = runtimeResult.Sum(x => x.time);
                var finalMemory = runtimeResult.Max(x => x.memory);

                _db.JudgeStatuses
                    .Where(x => x.Id == statusId)
                    .SetField(x => x.TimeUsedInMs).WithValue(finalTime)
                    .SetField(x => x.MemoryUsedInByte).WithValue(finalMemory)
                    .SetField(x => x.Result).WithValue((int)finalResult)
                    .Update();

                for (var i = 0; i < runtimeResult.Count(); i++)
                {
                    _db.SubJudgeStatuses
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
                UpdateUserProblemJson(userId, problem.Id, isAccepted);
            }

            _hub.Clients.All.InvokeAsync("ItemUpdated", "judge", statusId);
        }

        private void UpdateUserProblemJson(Guid userId, string problemId, bool isAccepted)
        {
            var effectedRows = 0;
            while (effectedRows == 0)
            {
                var user = _db.Users.AsNoTracking().Single(x => x.Id == userId);
                var timestamp = user.ConcurrencyStamp;
                if (!user.TriedProblems.Object.Contains(problemId))
                {
                    user.TriedProblems.Object.Add(problemId);
                    effectedRows = _db.Users
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
                    var user = _db.Users.AsNoTracking().Single(x => x.Id == userId);
                    var timestamp = user.ConcurrencyStamp;
                    if (!user.PassedProblems.Object.Contains(problemId))
                    {
                        user.PassedProblems.Object.Add(problemId);
                        effectedRows = _db.Users
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
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JudgeStateMachineHandlerExtension
    {
        public static IServiceCollection AddJudgeStateMachineHandler(this IServiceCollection self)
        {
            return self.AddScoped<JudgeStateMachineHandler>();
        }
    }
}