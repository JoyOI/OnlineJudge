﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Management
{
    [Route("management/[controller]")]
    public class VirtualJudgeAccountController : BaseController
    {
        [HttpPost("requestaccount")]
        public async Task<IActionResult> RequestAccount(CancellationToken token)
        {
            Guid stateMachineId = JsonConvert.DeserializeObject<dynamic>(RequestBody).id;
            var status = await DB.JudgeStatuses
                .Include(x => x.Problem)
                .FirstOrDefaultAsync(x => x.RelatedStateMachineIds.Any(y => y.StateMachineId == stateMachineId), token);

            if (status == null)
            {
                return Result(404, "Status not found");
            }

            if (status.Problem.Source == OnlineJudge.Models.ProblemSource.Bzoj)
            {
                var account = await DB.VirtualJudgeUsers
                    .SingleOrDefaultAsync(x => x.Source == OnlineJudge.Models.ProblemSource.Bzoj && x.LockerId == status.Id, token);

                while (account == null)
                {
                    var affectedRows = DB.VirtualJudgeUsers
                        .Where(x => x.Source == OnlineJudge.Models.ProblemSource.Bzoj && !x.LockerId.HasValue)
                        .Take(1)
                        .SetField(x => x.LockerId).WithValue(status.Id)
                        .Update();

                    if (affectedRows == 1)
                    {
                        account = await DB.VirtualJudgeUsers
                            .SingleOrDefaultAsync(x => x.Source == OnlineJudge.Models.ProblemSource.Bzoj && x.LockerId == status.Id, token);
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }

                return Result(new
                {
                    username = account.Username,
                    password = account.Password
                });
            }
            else
            {
                return Result(400, "Problem source not supported");
            }
        }
    }
}
