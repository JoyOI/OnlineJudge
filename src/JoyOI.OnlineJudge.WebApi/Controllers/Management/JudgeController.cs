using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoyOI.OnlineJudge.WebApi.Lib;

namespace JoyOI.OnlineJudge.WebApi.Controllers.ManagementServiceCallback
{
    [Route("management/[controller]")]
    public class JudgeController : BaseController
    {
        [HttpPost("stagechange/{id:Guid}")]
        public async Task<IActionResult> StageChange(
            [FromServices] JudgeStateMachineHandler handler,
            Guid id)
        {
            try
            {
                await handler.HandleJudgeResultAsync(id, default(CancellationToken));
            }
            catch (KeyNotFoundException ex)
            {
                return Result(404, ex.Message);
            }

            return Result(200, "ok");
        }
    }
}
