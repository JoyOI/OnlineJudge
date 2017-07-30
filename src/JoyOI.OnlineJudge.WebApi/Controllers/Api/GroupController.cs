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
    public class GroupController : BaseController
    {
        [HttpGet("all")]
        public Task<ApiResult<PagedResult<IEnumerable<Group>>>> Get(GroupType? type, string name, int? page, CancellationToken token)
        {
            IQueryable<Group> ret = DB.Groups;
            if (type.HasValue)
            {
                ret = ret.Where(x => x.Type == type.Value);
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                ret = ret.Where(x => x.Name.Contains(name) || name.Contains(x.Name));
            }
            if (!page.HasValue)
            {
                page = 1;
            }
            return Paged(ret, page.Value, 20, token);
        }

        [HttpGet("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult<Group>> Get(string id, CancellationToken token)
        {
            var ret = await DB.Groups.SingleOrDefaultAsync(x => x.Id == id, token);
            return Result(ret);
        }

        [HttpPost("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        [HttpPatch("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public void Patch(string id, [FromBody]string value)
        {
            var ret = 
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        #region Private Functions
        private async Task<bool> HasPermissionToGroupAsync(string problemId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.GroupEditPermission
                   && x.ClaimValue == problemId));
        #endregion
    }
}
