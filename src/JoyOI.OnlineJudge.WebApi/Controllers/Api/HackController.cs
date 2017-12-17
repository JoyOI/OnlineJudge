using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using JoyOI.OnlineJudge.ContestExecutor;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.ManagementService.SDK;
using Newtonsoft.Json;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class HackController : BaseController
    {
        [HttpGet("all")]
        public async Task<IActionResult> Get(
            HackResult? status, 
            string problemId, 
            string userId, 
            DateTime? begin, 
            DateTime? end, 
            string contestId, 
            int? page,
            [FromServices] ContestExecutorFactory cef,
            CancellationToken token)
        {
            IQueryable<HackStatus> ret = DB.HackStatuses;

            if (!page.HasValue)
            {
                page = 1;
            }

            if (status.HasValue)
            {
                ret = ret.Where(x => x.Result == status.Value);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                ret = ret.Where(x => x.User.UserName == userId);
            }

            if (begin.HasValue)
            {
                ret = ret.Where(x => x.Time >= begin.Value);
            }

            if (end.HasValue)
            {
                ret = ret.Where(x => x.Time <= end.Value);
            }

            if (!string.IsNullOrEmpty(contestId))
            {
                ret = ret.Where(x => x.ContestId == contestId);
            }

            var result = await DoPaging(ret.OrderByDescending(x => x.Time), page.Value, 20, token);
            if (!IsMasterOrHigher && result.data.result.Any(x => !string.IsNullOrWhiteSpace(x.ContestId)))
            {
                foreach (var x in result.data.result.Where(x => !string.IsNullOrWhiteSpace(x.ContestId)))
                {
                    var ce = cef.Create(x.ContestId);
                    var submittorUsername = DB.Users.Single(y => y.Id == x.UserId).UserName;
                    var isContestInProgress = ce.IsContestInProgress(User.Current?.UserName) || ce.IsContestInProgress(submittorUsername);
                    if (isContestInProgress && !ce.HasPermissionToContest())
                    {
                        ce.OnShowHackResult(x);
                    }
                    x.Contest = null;
                }
            }

            return Json(result);
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(
            Guid id,
            [FromServices] ContestExecutorFactory cef)
        {
            var ret = DB.HackStatuses
                .Include(x => x.User)
                .SingleOrDefault(x => x.Id == id);

            var username = ret.User.UserName;
            ret.User = null;

            if (ret == null)
            {
                return Result(404, "Hack status is not found");
            }

            if (!string.IsNullOrEmpty(ret.ContestId))
            {
                var ce = cef.Create(ret.ContestId);
                if (ce.IsContestInProgress(User.Current?.UserName) || ce.IsContestInProgress(username))
                {
                    ce.OnShowHackResult(ret);
                }
            }

            return Result(ret);
        }

        [HttpPut]
        public async Task<IActionResult> Put(
            [FromServices] ContestExecutorFactory cef, 
            [FromServices] ManagementServiceClient mgmt,
            CancellationToken token)
        {
            var request = JsonConvert.DeserializeObject<HackRequest>(RequestBody);
            var judge = await DB.JudgeStatuses
                .Include(x => x.Problem)
                .SingleOrDefaultAsync(x => x.Id == request.JudgeStatusId, token);

            if (judge == null)
            {
                return Result(404, "The judge status is not found");
            }

            if (!judge.BinaryBlobId.HasValue)
            {
                return Result(400, "The lagency status could not be hacked.");
            }

            if (!string.IsNullOrEmpty(request.ContestId))
            {
                var ce = cef.Create(request.ContestId);
                if (ce.IsStatusHackable(judge) && ce.IsContestInProgress())
                {
                    return Result(400, "You cannot hack this status");
                }
            }

            // Upload hack data to management service
            var blobId = await mgmt.PutBlobAsync("data.txt", request.IsBase64 ? Convert.FromBase64String(request.Data) : Encoding.UTF8.GetBytes(request.Data), token);

            var hack = new HackStatus
            {
                ContestId = request.ContestId,
                JudgeStatusId = judge.Id,
                HackDataBlobId = blobId,
                Result = HackResult.Pending,
                Time = DateTime.UtcNow,
                UserId = User.Current.Id,
                HackeeResult = JudgeResult.Pending
            };
            DB.HackStatuses.Add(hack);
            await DB.SaveChangesAsync(token);

            var blobs = new List<BlobInfo>(10);
            // Put the data into blob collection
            blobs.Add(new BlobInfo(blobId, "data.txt"));

            // Put the hackee program into blob collection
            blobs.Add(new BlobInfo(judge.BinaryBlobId.Value, "Hackee" + Constants.GetBinaryExtension(judge.Language)));

            // Put range validator program into blob collection
            if (judge.Problem.RangeBlobId.HasValue && string.IsNullOrEmpty(judge.Problem.RangeError))
            {
                blobs.Add(new BlobInfo(judge.Problem.RangeBlobId.Value, "Range" + Constants.GetBinaryExtension(judge.Problem.RangeLanguage)));
            }

            // Put standard program into blob collection
            if (judge.Problem.StandardBlobId.HasValue && string.IsNullOrEmpty(judge.Problem.StandardError))
            {
                blobs.Add(new BlobInfo(judge.Problem.StandardBlobId.Value, "Standard" + Constants.GetBinaryExtension(judge.Problem.StandardLanguage)));
            }
            else
            {
                // Get 3 accepted programs instead of standard program
                var statuses = await DB.JudgeStatuses
                    .Where(x => x.Result == JudgeResult.Accepted && x.ProblemId == judge.ProblemId && x.BinaryBlobId.HasValue)
                    .OrderBy(x => x.TimeUsedInMs)
                    .ThenBy(x => x.MemoryUsedInByte)
                    .Take(3)
                    .ToListAsync(token);

                if (statuses.Count == 0)
                {
                    return Result(400, "Missing standard program, this status could not be hacked");
                }

                var standards = statuses.Select(x => new BlobInfo(x.BinaryBlobId.Value, "Standard-" + x.Id.ToString().Substring(0,8) + Constants.GetBinaryExtension(x.Language), x.Id.ToString()));
                blobs.AddRange(standards);
            }

            // Put validator into blob collection
            if (judge.Problem.ValidatorBlobId.HasValue && string.IsNullOrEmpty(judge.Problem.ValidatorError))
            {
                blobs.Add(new BlobInfo(judge.Problem.ValidatorBlobId.Value, "Validator" + Constants.GetBinaryExtension(judge.Problem.ValidatorLanguage)));
            }
            else
            {
                blobs.Add(new BlobInfo(Guid.Parse(Configuration["JoyOI:StandardValidatorBlobId"]), "Validator.out"));
            }

            // Start hack state machine
            var stateMachineId = await mgmt.PutStateMachineInstanceAsync("HackZStateMachine", Configuration["ManagementService:CallBack"], blobs.ToArray(), 1, token);
            var stateMachine = new StateMachine { CreatedTime = DateTime.Now, Name = "HackStateMachine", Id = stateMachineId };
            DB.StateMachines.Add(stateMachine);
            hack.RelatedStateMachineIds.Add(new HackStatusStateMachine { StateMachineId = stateMachine.Id, StatusId = judge.Id });
            await DB.SaveChangesAsync(token);

            return Result(hack.Id);
        }

        [HttpGet("{id:Guid}/data")]
        public async Task<IActionResult> GetData(
            Guid id,
            [FromServices] ManagementServiceClient mgmt,
            [FromServices] ContestExecutorFactory cef,
            CancellationToken token)
        {
            var hack = await DB.HackStatuses
                .Include(x => x.Status)
                .SingleOrDefaultAsync(x => x.Id == id);

            if (hack == null)
            {
                return Result(404, "Hack status is not found");
            }

            if (!hack.HackDataBlobId.HasValue)
            {
                return Result(404, "The hack data is not found");
            }

            if (!string.IsNullOrEmpty(hack.ContestId))
            {
                var ce = cef.Create(hack.ContestId);
                if (!IsMasterOrHigher && !await HasPermissionToProblemAsync(hack.Status.ProblemId, token) && ce.IsContestInProgress() && User.Current?.Id != hack.UserId)
                {
                    return Result(401, "No permission");
                }
            }
            else
            {
                if (!IsMasterOrHigher && !await HasPermissionToProblemAsync(hack.Status.ProblemId, token) && User.Current?.Id != hack.UserId)
                {
                    return Result(401, "No permission");
                }
            }

            var blob = await mgmt.GetBlobAsync(hack.HackDataBlobId.Value, token);
            return Result(Encoding.UTF8.GetString(blob.Body));
        }

        #region Private Functions
        private async Task<bool> HasPermissionToProblemAsync(string problemId, CancellationToken token = default(CancellationToken))
            => User.Current != null && (await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
            || await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ProblemEditPermission
                   && x.ClaimValue == problemId));
        #endregion
    }
}
