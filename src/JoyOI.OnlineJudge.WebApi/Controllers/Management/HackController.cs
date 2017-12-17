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
    public class HackController : BaseController
    {
        [HttpPost("stagechange/{id:Guid}")]
        public async Task<IActionResult> StageChange(
            [FromServices] HackStateMachineHandler handler,
            Guid id)
        {
            await handler.HandleHackResultAsync(id, default(CancellationToken));
            return Result(200, "ok");
        }
    }
}
