using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class GroupController : BaseController
    {
        #region Group
        [HttpGet("all")]
        public Task<IActionResult> Get(string name, int? page, CancellationToken token)
        {
            IQueryable<Group> ret = DB.Groups;
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

        [HttpGet("cur")]
        public async Task<IActionResult> Get(CancellationToken token)
        {
            if (!IsGroupRequest())
            {
                return Result(404, "Group not found.");
            }
            else
            {
                return await Get(CurrentGroup.Id, token);
            }
        }

        [HttpGet("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Get(string id, CancellationToken token)
        {
            var ret = await DB.Groups.SingleOrDefaultAsync(x => x.Id == id, token);
            return Result(ret);
        }

        [HttpPost("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        [HttpPatch("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Patch(string id, CancellationToken token)
        {
            if (!await HasPermissionToSpecifiedGroupAsync(id, token))
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

                PatchEntity(group, RequestBody);
                await DB.SaveChangesAsync(token);
                return Result(200, "Patch Succeeded");
            }
        }
        
        [HttpPut("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Put(string id, CancellationToken token)
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

            var group = PutEntity<Group>(RequestBody).Entity;
            group.Id = id;
            group.CachedMemberCount = 1;
            DB.Groups.Add(group);

            DB.GroupMembers.Add(new GroupMember
            {
                CreatedTime = DateTime.UtcNow,
                GroupId = group.Id,
                Status = GroupMemberStatus.Approved,
                UserId = User.Current.Id
            });

            await DB.SaveChangesAsync(token);
            return Result(200, "Put Succeeded");
        }

        [HttpDelete("{id:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> Delete(string id, CancellationToken token)
        {
            if (!await HasPermissionToSpecifiedGroupAsync(id, token))
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
        #endregion

        #region Group Session
        [HttpGet("cur/session")]
        public async Task<IActionResult> GetSession(CancellationToken token)
        {
            if (!IsGroupRequest())
            {
                return Result(400, "Invalid request");
            }
            else
            {
                if (await IsGroupMemberAsync(token))
                {
                    return Result(new
                    {
                        IsMember = true,
                        IsMaster = await HasPermissionToGroupAsync()
                    });
                }
                else
                {
                    return Result(new
                    {
                        IsMember = false,
                        IsMaster = false
                    });
                }
            }
        }
        #endregion

        #region Group Problem
        [HttpPut("cur/problem/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> PutProblem(string problemId, CancellationToken token)
        {
            if (!IsGroupRequest())
            {
                return Result(400, "Invalid Request");
            }
            else if (!await HasPermissionToSpecifiedGroupAsync(CurrentGroup.Id, token))
            {
                return Result(400, "No permission");
            }
            else if (await DB.GroupProblems.AnyAsync(x => x.ProblemId == problemId && x.GroupId == CurrentGroup.Id, token))
            {
                return Result(400, "The problem was already existed.");
            }
            else
            {
                DB.GroupProblems.Add(new GroupProblem
                {
                    GroupId = CurrentGroup.Id,
                    ProblemId = problemId
                });

                await DB.SaveChangesAsync(token);
                return Result(200, "Succeeded");
            }
        }

        [HttpDelete("cur/problem/{problemId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> DeleteProblem(string problemId, CancellationToken token)
        {
            if (!IsGroupRequest())
            {
                return Result(400, "Invalid Request");
            }
            else if (!await HasPermissionToSpecifiedGroupAsync(CurrentGroup.Id, token))
            {
                return Result(400, "No permission");
            }
            else if (!await DB.GroupProblems.AnyAsync(x => x.ProblemId == problemId && x.GroupId == CurrentGroup.Id, token))
            {
                return Result(400, "The problem was not found.");
            }
            else
            {
                DB.GroupProblems
                    .Where(x => x.GroupId == CurrentGroup.Id)
                    .Where(x => x.ProblemId == problemId)
                    .Delete();
                
                return Result(200, "Succeeded");
            }
        }
        #endregion

        #region Group Contest Reference
        [HttpPut("cur/contest/{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> PutContest(string contestId, CancellationToken token)
        {
            if (!IsGroupRequest())
            {
                return Result(400, "Invalid Request");
            }
            if (!await DB.Contests.AnyAsync(x => x.Id == contestId))
            {
                return Result(404, "Contest was not found");
            }
            else if (!await HasPermissionToSpecifiedGroupAsync(CurrentGroup.Id, token))
            {
                return Result(400, "No permission");
            }
            else if (await DB.GroupContestReferences.AnyAsync(x => x.ContestId == contestId && x.GroupId == CurrentGroup.Id, token))
            {
                return Result(400, "The contest was already existed.");
            }
            else
            {
                DB.GroupContestReferences.Add(new GroupContestReference
                {
                    GroupId = CurrentGroup.Id,
                    ContestId = contestId
                });

                await DB.SaveChangesAsync(token);
                return Result(200, "Succeeded");
            }
        }

        [HttpDelete("cur/contest/{contestId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}")]
        public async Task<IActionResult> DeleteContest(string contestId, CancellationToken token)
        {
            if (!IsGroupRequest())
            {
                return Result(400, "Invalid Request");
            }
            else if (!await HasPermissionToSpecifiedGroupAsync(CurrentGroup.Id, token))
            {
                return Result(400, "No permission");
            }
            else if (!await DB.GroupContestReferences.AnyAsync(x => x.ContestId == contestId && x.GroupId == CurrentGroup.Id, token))
            {
                return Result(400, "The contest was not found.");
            }
            else
            {
                DB.GroupContestReferences
                    .Where(x => x.GroupId == CurrentGroup.Id)
                    .Where(x => x.ContestId == contestId)
                    .Delete();

                return Result(200, "Succeeded");
            }
        }
        #endregion

        #region Group Member
        [HttpGet("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/member/all")]
        public async Task<IActionResult> GetMember(string groupId, GroupMemberStatus? status, int? page, CancellationToken token)
        {
            var managers = await DB.UserClaims
                .Where(x => x.ClaimType == Constants.GroupEditPermission)
                .Where(x => x.ClaimValue == groupId)
                .Select(x => x.UserId)
                .ToListAsync(token);

            IQueryable<GroupMember> ret = DB.GroupMembers
                .Where(x => x.GroupId == groupId);

            if (status.HasValue)
            {
                ret = ret.Where(x => x.Status == status.Value);
            }

            ret = ret
                .OrderByDescending(x => managers.Contains(x.UserId))
                .ThenBy(x => x.CreatedTime);

            if (!page.HasValue)
            {
                page = 1;
            }

            var hasPermissionToGroup = await HasPermissionToSpecifiedGroupAsync(groupId, token);

            var result = await DoPaging(ret.Select(x => new GroupMemberViewModel
            {
                IsMaster = managers.Contains(x.UserId),
                Status = x.Status,
                UserId = x.UserId,
                JoinedTime = x.CreatedTime,
                Request = hasPermissionToGroup ? x.Message : null,
                Response = hasPermissionToGroup ? x.Feedback : null
            }), page.Value, 50, token);

            return Json(result);
        }
        
        [HttpPut("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/member/{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> PutMember(string groupId, string username, CancellationToken token)
        {
            var user = await User.Manager.FindByNameAsync(username);
            var group = await DB.Groups.SingleOrDefaultAsync(x => x.Id == groupId, token);
            if (group == null)
            {
                return Result(404, "Group Not Found");
            }
            else if (username != User.Current.UserName && !await HasPermissionToSpecifiedGroupAsync(groupId))
            {
                return Result(401, "No permission");
            }
            else if (await DB.GroupMembers.AnyAsync(x => x.GroupId == groupId && x.UserId == user.Id && x.Status == GroupMemberStatus.Approved, token))
            {
                return Result(400, "Already joined the group.");
            }
            else if (group.JoinMethod == GroupJoinMethod.Verification && await DB.GroupMembers.AnyAsync(x => x.GroupId == groupId && x.UserId == user.Id && x.Status == GroupMemberStatus.Pending, token))
            {
                return Result(400, "Already submitted the request.");
            }
            else
            {
                DB.GroupMembers
                    .Where(x => x.GroupId == groupId && x.UserId == user.Id)
                    .Delete();

                var value = RequestBody;
                var groupMember = PutEntity<GroupMember>(value).Entity;
                groupMember.GroupId = groupId;
                groupMember.UserId = user.Id;
                if (await HasPermissionToGroupAsync(token) || group.JoinMethod == GroupJoinMethod.Everyone)
                {
                    groupMember.Status = GroupMemberStatus.Approved;
                }
                groupMember.CreatedTime = DateTime.UtcNow;
                DB.GroupMembers.Add(groupMember);
                await DB.SaveChangesAsync(token);
                return Result(200, "Put Succeeded");
            }
        }

        [HttpPost("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/member/{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        [HttpPatch("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/member/{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> PatchMember(string groupId, string username, CancellationToken token)
        {
            var request = JsonConvert.DeserializeObject<GroupMemberPatchModel>(RequestBody);

            if (!await HasPermissionToSpecifiedGroupAsync(groupId, token))
            {
                return Result(401, "No permission");
            }

            var user = await User.Manager.FindByNameAsync(username);

            var groupMember = await DB.GroupMembers.SingleOrDefaultAsync(x => x.GroupId == groupId && x.UserId == user.Id, token);
            if (groupMember == null)
            {
                return Result(404, "Member Not Found");
            }

            groupMember.Feedback = request.Response;
            groupMember.Status = request.Status;
            await DB.SaveChangesAsync(token);
            return Result(200, "Patch Succeeded");
        }

        [HttpDelete("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/member/{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> DeleteMember(string groupId, string username, CancellationToken token)
        {
            var user = await User.Manager.FindByNameAsync(username);

            if (user.Id != User.Current.Id && !await HasPermissionToSpecifiedGroupAsync(groupId, token))
            {
                return Result(401, "No permission");
            }

            if (await DB.UserClaims.AnyAsync(x => x.ClaimType == Constants.GroupEditPermission && x.ClaimValue == groupId && x.UserId == user.Id, token))
            {
                return Result(400, "Cannot remove an owner from the group");
            }

            await DB.GroupMembers
                .Where(x => x.UserId == user.Id)
                .Where(x => x.GroupId == groupId)
                .DeleteAsync(token);

            return Result(200, "Delete succeeded");
        }
        #endregion

        #region Claims
        [HttpGet("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/all")]
        public async Task<IActionResult> GetClaims(string groupId, CancellationToken token)
        {
            var ret = await DB.UserClaims
                .Where(x => x.ClaimType == Constants.GroupEditPermission)
                .Where(x => x.ClaimValue == groupId)
                .ToListAsync(token);
            return Result(ret);
        }

        [HttpPut("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> PutClaims(string groupId, string username, CancellationToken token)
        {
            var user = await User.Manager.FindByNameAsync(username);
            if (!await HasPermissionToSpecifiedGroupAsync(groupId, token))
            {
                return Result(401, "No permission");
            }
            else if (await DB.UserClaims.AnyAsync(x => x.ClaimValue == groupId && x.ClaimType == Constants.GroupEditPermission && x.UserId == user.Id, token))
            {
                return Result(400, "Already exists");
            }
            else
            {
                DB.UserClaims.Add(new IdentityUserClaim<Guid>
                {
                    ClaimType = Constants.GroupEditPermission,
                    UserId = user.Id,
                    ClaimValue = groupId
                });
                await DB.SaveChangesAsync(token);
                return Result(200, "Succeeded");
            }
        }

        [HttpDelete("{groupId:regex(^[[a-zA-Z0-9-_]]{{4,128}}$)}/claim/{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> DeleteClaim(string username, string groupId, CancellationToken token)
        {
            var user = await User.Manager.FindByNameAsync(username);
            if (!await HasPermissionToSpecifiedGroupAsync(groupId, token))
            {
                return Result(401, "No permission");
            }
            else if (!await DB.UserClaims.AnyAsync(x => x.ClaimValue == groupId && x.ClaimType == Constants.GroupEditPermission && x.UserId == user.Id, token))
            {
                return Result(404, "Claim not found");
            }
            else if (username == User.Current.UserName)
            {
                return Result(400, "Cannot remove yourself");
            }
            else
            {
                await DB.UserClaims
                    .Where(x => x.ClaimValue == groupId && x.ClaimType == Constants.GroupEditPermission && x.UserId == user.Id)
                    .DeleteAsync(token);

                return Result(200, "Delete succeeded");
            }
        }
        #endregion

        #region Private Functions
        private Task<bool> IsMemberOfGroup(string groupId, Guid userId, CancellationToken token)
            => DB.GroupMembers.AnyAsync(x => x.GroupId == groupId && x.UserId == userId, token);
        #endregion
    }
}
