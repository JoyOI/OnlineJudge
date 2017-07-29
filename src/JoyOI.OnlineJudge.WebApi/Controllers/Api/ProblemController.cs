using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class ProblemController : BaseController
    {
        [HttpGet("all")]
        [HttpGet("all/page/{page:int?}")]
        public async Task<ApiResult<PagedResult<IEnumerable<Problem>>>> Get(string title, int? difficulty, string tag, int? page, CancellationToken token)
        {
            IQueryable<Problem> ret = DB.Problems;

            if (User.Current == null || !await User.Manager.IsInAnyRolesAsync(User.Current, "Root, Master"))
            {
                var editableProblemIds = await DB.UserClaims
                    .Where(x => x.UserId == User.Current.Id && x.ClaimType == ClaimConstants.ProblemEditPermission)
                    .Select(x => x.ClaimValue)
                    .ToListAsync(token);

                ret = ret.Where(x => x.IsVisiable || editableProblemIds.Contains(x.Id));
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                ret = ret.Where(x => x.Title.Contains(title) || title.Contains(x.Title));
            }

            if (difficulty.HasValue)
            {
                ret = ret.Where(x => x.Difficulty == difficulty.Value);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var tags = tag.Split(",").Select(x => x.Trim());
                if (tags.Count() > 0)
                {
                    ret = ret.Where(ContainsWhere<Problem>(x => x.Tags, tags));
                }
            }

            if (!page.HasValue)
            {
                page = 1;
            }

            return await Paged(ret, page.Value, 100, token);
        }
        
        [HttpGet("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult<Problem>> Get(string id, CancellationToken token)
        {
            var ret = await DB.Problems.SingleOrDefaultAsync(x => x.Id == id, token);
            if (ret == null)
            {
                return await Result<Problem>(404, "Not Found");
            }
            else
            {
                return await Result(ret);
            }
        }
        
        [HttpPatch("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Patch(string id, [FromBody]string value, CancellationToken token)
        {
            if (User.Current == null 
                || !await User.Manager.IsInAnyRolesAsync(User.Current, ClaimConstants.MasterOrHigherRoles) 
                && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id 
                    && x.ClaimType == ClaimConstants.ProblemEditPermission
                    && x.ClaimValue == id, token))
            {
                return await Result(401, "No Permission");
            }
            else
            {
                var problem = await DB.Problems.SingleOrDefaultAsync(x => x.Id == id, token);
                PatchEntity(value, problem);
                await DB.SaveChangesAsync(token);
                return await Result(200, "Patch Succeeded");
            }
        }
        
        [HttpPut("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Put(string id, [FromBody]Problem value, CancellationToken token)
        {
            if (await DB.Problems.AnyAsync(x => x.Id == id, token))
            {
                return await Result(403, "The problem id is already exists.");
            }

            DB.Problems.Add(value);
            await DB.SaveChangesAsync(token);
            return await Result(200, "Put Succeeded");
        }
        
        [HttpDelete("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Delete(string id, CancellationToken token)
        {
            if (User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, ClaimConstants.MasterOrHigherRoles)
               && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == ClaimConstants.ProblemEditPermission
                   && x.ClaimValue == id, token))
            {
                return await Result(401, "No Permission");
            }
            else
            {
                await DB.UserClaims
                    .Where(x => x.ClaimType == ClaimConstants.ProblemEditPermission)
                    .Where(x => x.ClaimValue == id)
                    .DeleteAsync(token);
                await DB.Problems
                    .Where(x => x.Id == id)
                    .DeleteAsync(token);
                return await Result(200, "Delete Succeeded");
            }
        }

        [HttpGet("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/all")]
        public async Task<ApiResult<List<TestCase>>> GetTestCase(string problemid, TestCaseType? type, CancellationToken token)
        {
            IQueryable<TestCase> ret = DB.TestCases
                .Where(x => x.ProblemId == problemid);

            if (type.HasValue)
            {
                ret = ret.Where(x => x.Type == type.Value);
            }

            return await Result(await ret.ToListAsync(token));
        }

        [HttpGet("{problemid:(^[a-zA-Z0-9-_ ]{4,128}$)}/testcase/{id:Guid}")]
        public async Task<ApiResult<TestCase>> GetTestCase(string problemid, Guid id, CancellationToken token)
        {
            var ret = await DB.TestCases.SingleOrDefaultAsync(x => x.ProblemId == problemid && x.Id == id, token);
            if (ret == null)
            {
                return await Result<TestCase>(404, "Not Found");
            }
            else if (User.Current == null
                || !await DB.TestCaseBuyLogs.AnyAsync(x => x.TestCaseId == id && x.UserId == User.Current.Id, token)
                || !await User.Manager.IsInAnyRolesAsync(User.Current, ClaimConstants.MasterOrHigherRoles)
                && await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                    && x.ClaimType == ClaimConstants.ProblemEditPermission
                    && x.ClaimValue == problemid, token))
            {
                return await Result<TestCase>(401, "No Permission");
            }
            else
            {
                return await Result(ret);
            }
        }
    }
}
