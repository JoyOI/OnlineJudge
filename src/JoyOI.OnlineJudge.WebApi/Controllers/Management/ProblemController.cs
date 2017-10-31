using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JoyOI.ManagementService.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.ManagementServiceCallback
{
    [Route("management/[controller]")]
    public class ProblemController : BaseController
    {
        [HttpPost("Bzoj/{id:Guid}")]
        public async Task<IActionResult> Bzoj(
            [FromServices] ManagementServiceClient MgmtSvc,
            Guid id)
        {
            var statemachine = await MgmtSvc.GetStateMachineInstanceAsync(id, default(CancellationToken));
            var actors = statemachine.StartedActors.Where(x => x.Name == "BzojPullProblemBodyActor" && x.Outputs.Any(y => y.Name == "problem.json")).ToDictionary(x => "bzoj-" + x.Tag);
            var ids = actors.Keys.Select(x => x).ToList();

            foreach (var i in ids)
            {
                var model = JsonConvert.DeserializeObject<ProblemJson>(Encoding.UTF8.GetString((await ManagementService.GetBlobAsync(actors[i].Outputs.Single(x => x.Name == "problem.json").Id)).Body));
                var problem = await DB.Problems.SingleOrDefaultAsync(x => x.Id == i);
                if (problem == null)
                {
                    problem = new Problem
                    {
                        Title = model.Title,
                        Id = i,
                        Body = model.Body,
                        Source = ProblemSource.Bzoj,
                        Tags = "按题库:Bzoj",
                        TimeLimitationPerCaseInMs = model.TimeLimitInMs,
                        MemoryLimitationPerCaseInByte = model.MemoryLimitInByte,
                        IsVisiable = true
                    };
                    DB.Problems.Add(problem);
                }
                else
                {
                    problem.Title = model.Title;
                    problem.Body = model.Body;
                    problem.TimeLimitationPerCaseInMs = model.TimeLimitInMs;
                    problem.MemoryLimitationPerCaseInByte = model.MemoryLimitInByte;
                    problem.IsVisiable = true;
                }
                await DB.SaveChangesAsync();
            }

            return Result(200, "ok");
        }
    }
}
