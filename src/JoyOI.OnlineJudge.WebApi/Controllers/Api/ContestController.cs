using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;
namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ContestController : BaseController
    {
        [HttpGet("all")]
        [HttpGet("all/page/{page:int}")]
        public Task<ApiResult<PagedResult<IEnumerable<Contest>>>> Get(
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

        [HttpGet("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult<Contest>> Get(string id, CancellationToken token)
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

        // POST api/values
        [HttpPost("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        [HttpPatch("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Patch(string id, [FromBody]string value, CancellationToken token)
        {
            if (!await HasPermissionToContestAsync(id, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                var contest = DB.Contests
                    .SingleOrDefaultAsync(x => x.Id == id, token);

                if (contest == null)
                {
                    return Result(404, "Contest not found");
                }

                PatchEntity(contest, value);
                await DB.SaveChangesAsync(token);

                return Result(200, "Patch succeeded");
            }
        }

        //[HttpPut("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        //public async Task<ApiResult> Put(string id, [FromBody]string value, CancellationToken token)
        //{
        //    if (await DB.Contests.AnyAsync(x => x.Id == id, token))
        //    {
        //        return Result(400, "The problem id is already exists.");
        //    }
        //    else
        //    {

        //    }
        //}

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        #region Private Functions
        private async Task<bool> HasPermissionToContestAsync(string contestId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.ContestEditPermission
                   && x.ClaimValue == contestId));
        #endregion
    }
}
