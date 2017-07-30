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
        public async Task<ApiResult> Patch(string id, [FromBody]string value, CancellationToken token)
        {
            if (!await HasPermissionToGroupAsync(id, token))
            {
                return Result(401, "No permission");
            }
            else
            {
                var group = await DB.Groups.SingleOrDefaultAsync(x => x.Id == id, token);
                if (group == null)
                {
                    return Result(404, "Not Found");
                }

                PatchEntity(value, group);
                await DB.SaveChangesAsync(token);
                return Result(200, "Patch Succeeded");
            }
        }
        
        [HttpPut("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Put(string id, [FromBody]Group value, CancellationToken token)
        {
            if (await DB.Groups.AnyAsync(x => x.Id == id, token))
            {
                return Result(403, "The problem id is already exists.");
            }

            if (await DB.UserClaims.CountAsync(x => x.ClaimType == Constants.GroupEditPermission
                && x.ClaimValue == id
                && x.UserId == User.Current.Id, token) >= 5)
            {
                return Result(400, "You cannot own more than 5 groups.");
            }

            FilterEntity(value);
            value.Id = id;
            DB.Groups.Add(value);

            DB.GroupMembers.Add(new GroupMember
            {
                CreatedTime = DateTime.Now,
                GroupId = value.Id,
                Status = GroupJoinRequestStatus.Approved,
                UserId = User.Current.Id
            });

            await DB.SaveChangesAsync(token);
            return Result(200, "Put Succeeded");
        }

        [HttpDelete("{id:(^[a-zA-Z0-9-_ ]{4,128}$)}")]
        public async Task<ApiResult> Delete(string id, CancellationToken token)
        {
            if (!await HasPermissionToGroupAsync(id, token))
            {
                return Result(401, "No Permission");
            }
            else
            {
                await DB.UserClaims
                    .Where(x => x.ClaimType == Constants.GroupEditPermission)
                    .Where(x => x.ClaimValue == id)
                    .DeleteAsync(token);
                await DB.Groups
                    .Where(x => x.Id == id)
                    .DeleteAsync(token);
                return Result(200, "Delete Succeeded");
            }
        }

        #region Private Functions
        private async Task<bool> HasPermissionToGroupAsync(string groupId, CancellationToken token = default(CancellationToken))
            => !(User.Current == null
               || !await User.Manager.IsInAnyRolesAsync(User.Current, Constants.MasterOrHigherRoles)
               && !await DB.UserClaims.AnyAsync(x => x.UserId == User.Current.Id
                   && x.ClaimType == Constants.GroupEditPermission
                   && x.ClaimValue == groupId));
        #endregion
    }
}
