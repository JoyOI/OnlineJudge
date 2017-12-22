using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using JoyOI.OnlineJudge.Models;
using JoyOI.ManagementService.SDK;
using JoyOI.ManagementService.Model.Dtos;
using JoyOI.OnlineJudge.ContestExecutor;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.OnlineJudge.WebApi.Hubs;
using JoyOI.OnlineJudge.WebApi.Lib;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    public class HackStateMachineHandler
    {
        private OnlineJudgeContext _db;

        private IHubContext<OnlineJudgeHub> _hub;

        private ManagementServiceClient _mgmt;

        private ContestExecutorFactory _cef;

        public HackStateMachineHandler(OnlineJudgeContext db, IHubContext<OnlineJudgeHub> hub, ManagementServiceClient mgmt, ContestExecutorFactory cef)
        {
            _db = db;
            _hub = hub;
            _mgmt = mgmt;
            _cef = cef;
        }

        public async Task HandleHackResultAsync(
            Guid statemachineId,
            CancellationToken token)
        {
            var statemachine = await _mgmt.GetStateMachineInstanceAsync(statemachineId, token);
            if (statemachine.Name == "HackStateMachine")
            {
                await HandlePlainHackAsync(statemachine, token);
            }
            else if (statemachine.Name == "HackAllStateMachine") // Contest only
            {
                await HandleContestHackAsync(statemachine, token);
            }
            else
            {
                throw new InvalidOperationException(statemachine.Name + " is invalid");
            }
        }

        private async Task HandlePlainHackAsync(StateMachineInstanceOutputDto statemachine, CancellationToken token)
        {
            var hack = await _db.HackStatuses
                .Include(x => x.RelatedStateMachineIds)
                .Include(x => x.Status)
                .ThenInclude(x => x.Problem)
                .FirstOrDefaultAsync(x => x.RelatedStateMachineIds.Any(y => y.StateMachineId == statemachine.Id), token);
            
            if (hack != null)
            {
                await HandleSingleHackAsync(statemachine.StartedActors.Where(x => x.Tag == "Hackee=" + hack.JudgeStatusId), hack, hack.Status, hack.Status.Problem, null, token);

                if (!string.IsNullOrEmpty(hack.ContestId))
                {
                    var ce = _cef.Create(hack.ContestId);
                    if (ce.PushNotificationSetting == PushNotificationType.All)
                    {
                        await _hub.Clients.All.InvokeAsync("ItemUpdated", "hack", hack.Id, hack.Status.UserId);
                    }

                    ce.OnHackCompleted(hack);
                }
                else
                {
                    await _hub.Clients.All.InvokeAsync("ItemUpdated", "hack", hack.Id, hack.Status.UserId);
                }
            }
        }

        private async Task HandleContestHackAsync(StateMachineInstanceOutputDto statemachine, CancellationToken token)
        {
            var actors = statemachine
                .StartedActors
                .GroupBy(x => x.Tag)
                .Where(x => x.Key.StartsWith("Hackee="))
                .ToList();

            if (actors.Count > 0)
            {
                var judgeStatusId = Guid.Parse(actors.First().Key.Split('=')[1]);
                var judge = await _db.JudgeStatuses
                    .Include(x => x.Problem)
                    .SingleAsync(x => x.Id == judgeStatusId, token);
                var problem = judge.Problem;
                var testCaseId = Guid.Parse(statemachine.InitialBlobs.Single(x => x.Name == "data.txt").Tag);

                foreach (var x in actors)
                {
                    await HandleSingleHackAsync(x, null, null, problem, testCaseId, token);
                }
            }
        }

        private async Task HandleSingleHackAsync(IEnumerable<ActorInfo> actors, HackStatus hack, JudgeStatus judge, Problem problem, Guid? testCaseId, CancellationToken token)
        {
            var sub = new SubJudgeStatus();
            var isBadData = false;

            if (actors.Count(x => x.Name == "CompareActor") == 0)
            {
                var runActor = actors.First(x => x.Stage == "GenerateHackeeAnswer");
                var runner = await _mgmt.ReadBlobAsObjectAsync<Runner>(runActor.Outputs.Single(x => x.Name == "runner.json").Id, token);

                if (runner.IsTimeout)
                {
                    sub.TimeUsedInMs = problem.TimeLimitationPerCaseInMs;
                    sub.MemoryUsedInByte = runner.PeakMemory;
                    sub.Result = JudgeResult.TimeExceeded;
                    sub.Hint = string.Join(Environment.NewLine, runActor.Exceptions) + Environment.NewLine + runner.Error;
                }
                else if (runner.ExitCode == 139 || runActor.Exceptions.Any(x => x.Contains("May cause by out of memory")) || runner.Error.Contains("std::bad_alloc") || runner.PeakMemory > problem.MemoryLimitationPerCaseInByte)
                {
                    sub.TimeUsedInMs = runner.UserTime;
                    sub.MemoryUsedInByte = problem.MemoryLimitationPerCaseInByte;
                    sub.Result = JudgeResult.MemoryExceeded;
                    sub.Hint = string.Join(Environment.NewLine, runActor.Exceptions) + Environment.NewLine + runner.Error;
                }
                else if (runner.ExitCode != 0)
                {
                    sub.TimeUsedInMs = runner.UserTime;
                    sub.MemoryUsedInByte = runner.PeakMemory;
                    sub.Result = JudgeResult.RuntimeError;
                    sub.Hint = string.Join(Environment.NewLine, runActor.Exceptions) + Environment.NewLine + runner.Error + Environment.NewLine + $"User process exited with code { runner.ExitCode }";
                }
            }
            else if (actors.Count(x => x.Name == "CompareActor") > 0)
            {
                var runners = actors.Where(x => x.Name == "CompareActor").Select(x => x.Outputs.Single(y => y.Name == "runner.json"));
                var runnerResults = runners.Select(x => _mgmt.ReadBlobAsObjectAsync<Runner>(x.Id, token).Result);
                var exitCodes = runnerResults.Select(x => x.ExitCode);
                if (exitCodes.Distinct().Count() > 1)
                {
                    isBadData = true;
                }
                else
                {
                    var runActor = actors.First(x => x.Stage == "GenerateHackeeAnswer");
                    var runRunner = await _mgmt.ReadBlobAsObjectAsync<Runner>(runActor.Outputs.Single(x => x.Name == "runner.json").Id, token);

                    var compareActor = actors.Last(x => x.Name == "CompareActor");
                    var runner = await _mgmt.ReadBlobAsObjectAsync<Runner>(compareActor.Outputs.Single(x => x.Name == "runner.json").Id, token);
                    if (runner.ExitCode < 0 || runner.ExitCode > 2)
                    {
                        sub.TimeUsedInMs = 0;
                        sub.MemoryUsedInByte = 0;
                        sub.Result = JudgeResult.SystemError;
                        sub.Hint = string.Join(Environment.NewLine, runActor.Exceptions) + Environment.NewLine + runner.Error;
                    }
                    else
                    {
                        sub.TimeUsedInMs = runRunner.UserTime;
                        sub.MemoryUsedInByte = runRunner.PeakMemory;
                        sub.Result = (JudgeResult)runner.ExitCode;
                        sub.Hint = string.Join(Environment.NewLine, runActor.Exceptions) + Environment.NewLine + runner.Error;
                    }
                }
            }
            else
            {
                isBadData = true;
            }

            if (hack == null && actors.Count(x => x.Stage == "GenerateHackeeAnswer") > 0 && testCaseId.HasValue)
            {
                var runActor = actors.First(x => x.Stage == "GenerateHackeeAnswer");
                var statusId = Guid.Parse(runActor.Tag.Split("=")[1]);
                var testCase = await _db.TestCases.SingleAsync(x => x.Id == testCaseId.Value, token);
                var testCases = await _db.TestCases
                    .Where(x => x.ProblemId == problem.Id)
                    .Select(x => x.Id)
                    .ToListAsync(token);

                sub.StatusId = statusId;
                sub.TestCaseId = testCaseId.Value;
                sub.InputBlobId = testCase.InputBlobId;
                sub.OutputBlobId = testCase.OutputBlobId;
                sub.SubId = testCases.IndexOf(testCaseId.Value);
                _db.SubJudgeStatuses.Add(sub);
                _db.SaveChanges();
            }

            if (hack != null)
            {
                _db.HackStatuses
                    .Where(x => x.Id == hack.Id)
                    .SetField(x => x.HackeeResult).WithValue(isBadData ? JudgeResult.Accepted : sub.Result)
                    .SetField(x => x.TimeUsedInMs).WithValue(sub.TimeUsedInMs)
                    .SetField(x => x.MemoryUsedInByte).WithValue(sub.MemoryUsedInByte)
                    .SetField(x => x.Result).WithValue(isBadData ? (HackResult.BadData) : (sub.Result == JudgeResult.Accepted ? HackResult.Failed : HackResult.Succeeded))
                    .Update();
            }
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HackStateMachineHandlerExtension
    {
        public static IServiceCollection AddHackStateMachineHandler(this IServiceCollection self)
        {
            return self.AddScoped<HackStateMachineHandler>();
        }
    }
}