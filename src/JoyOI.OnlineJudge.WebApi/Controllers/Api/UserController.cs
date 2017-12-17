using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JoyOI.UserCenter.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;
using JoyOI.OnlineJudge.WebApi.Lib;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private static Regex CookieExpireRegex = new Regex("(?<=; expires=)[0-9a-zA-Z: -/]{1,}(?=; path=)");

        #region User
        [HttpGet("all")]
        public async Task<IActionResult> Get(
            [FromServices] JoyOIUC UC,
            int? page, 
            string username, 
            CancellationToken token)
        {
            IQueryable<User> ret = DB.Users;

            if (!string.IsNullOrWhiteSpace(username))
            {
                ret = ret.Where(x => x.UserName.Contains(username) || username.Contains(x.UserName));
            }

            var result = await DoPaging(ret, page ?? 1, 50, token);
            var type = typeof(IdentityUser<Guid>);
            foreach (var x in result.data.result)
                foreach (var y in type.GetProperties().Where(y => y.Name != nameof(IdentityUser.Id) && y.Name != nameof(IdentityUser.UserName)))
                    y.SetValue(x, y.PropertyType.IsValueType ? Activator.CreateInstance(y.PropertyType) : null);

            return Json(result);
        }

        [HttpGet("{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> Get(string username, CancellationToken token)
        {
            var user = await DB.Users.SingleOrDefaultAsync(x => x.UserName == username, token);

            if (user == null)
            {
                return Result(404, "User not found");
            }

            var ret = new UserViewModel
            {
                activeTime = user.ActiveTime,
                avatarUrl = user.AvatarUrl,
                id = user.Id,
                passedProblems = user.PassedProblems.Object,
                registeryTime = user.RegisteryTime,
                role = (await User.Manager.GetRolesAsync(user)).FirstOrDefault(),
                triedProblems = user.TriedProblems.Object,
                username = user.UserName,
                motto = user.Motto,
                preferredLanguage = user.PreferredLanguage
            };

            return Result(ret);
        }

        [HttpGet("role")]
        public async Task<IActionResult> GetUserRoles(string usernames, string userids, CancellationToken token)
        {
            object ret = null;
            var roles = await DB.Roles.ToDictionaryAsync(x => x.Id, x => x.Name, token);

            if (!string.IsNullOrWhiteSpace(usernames))
            {
                var users = usernames.Split(',').Select(x => x.Trim());
                ret = (from u in DB.Users
                    where users.Contains(u.UserName)
                    let role = DB.UserRoles.Where(x => x.UserId == u.Id).FirstOrDefault()
                    select new { id = u.Id, username = u.UserName, avatarUrl = u.AvatarUrl, role = role == null ? null : roles[role.RoleId] })
                    .ToDictionary(x => x.username);
            }
            else if (!string.IsNullOrWhiteSpace(userids))
            {
                var ids = userids.Split(',').Select(x => Guid.Parse(x.Trim()));
                ret = (from u in DB.Users
                    where ids.Contains(u.Id)
                    let role = DB.UserRoles.Where(x => x.UserId == u.Id).FirstOrDefault()
                    select new { id = u.Id, username = u.UserName, avatarUrl = u.AvatarUrl, role = role == null ? null : roles[role.RoleId] })
                    .ToDictionary(x => x.id.ToString());
            }

            return Result(ret);
        }

        [HttpPost("{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        [HttpPatch("{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}")]
        public async Task<IActionResult> Patch(string username, CancellationToken token)
        {
            var user = await DB.Users.SingleOrDefaultAsync(x => x.UserName == username, token);
            if (user == null)
            {
                return Result(404, "The user is not found");
            }
            else if (User.Current.Id != user.Id && !IsMasterOrHigher)
            {
                return Result(403, "No permission");
            }
            else
            {
                PatchEntity(user, RequestBody);
                await DB.SaveChangesAsync(token);
                return Result(200, "Patched successfully.");
            }
        }
        #endregion

        #region Session
        [HttpGet("session/info")]
        public async Task<IActionResult> GetSessionInfo([FromServices] JoyOIUC UC, CancellationToken token)
        {
            var ret = new Dictionary<string, object>();
            ret.Add("isSignedIn", User.Current != null);
            if (User.Current != null)
            {
                var roles = await User.Manager.GetRolesAsync(User.Current);
                ret.Add("id", User.Current.Id);
                ret.Add("username", User.Current.UserName);
                ret.Add("email", User.Current.Email);
                ret.Add("role", roles.Count > 0 ? roles.First() : "Member");
                ret.Add("tried", User.Current.TriedProblems.Object);
                ret.Add("passed", User.Current.PassedProblems.Object);
                ret.Add("chat", UC.GenerateChatWindowUrl(User.Current.OpenId));
                ret.Add("preferredLanguage", User.Current.PreferredLanguage);
            }
            return Result<dynamic>(ret);
        }

        [HttpPut("session")]
        public async Task<IActionResult> PutSession([FromServices] JoyOIUC UC, CancellationToken token)
        {
            var login = JsonConvert.DeserializeObject<Login>(RequestBody);
            var authorizeResult = await UC.TrustedAuthorizeAsync(login.Username, login.Password);
            if (authorizeResult.succeeded)
            {
                var profileResult = await UC.GetUserProfileAsync(authorizeResult.data.open_id, authorizeResult.data.access_token);

                User user = await UserManager.FindByNameAsync(login.Username);

                if (user == null)
                {
                    user = new User
                    {
                        Id = authorizeResult.data.open_id,
                        UserName = login.Username,
                        Email = profileResult.data.email,
                        PhoneNumber = profileResult.data.phone,
                        AccessToken = authorizeResult.data.access_token,
                        ExpireTime = authorizeResult.data.expire_time,
                        OpenId = authorizeResult.data.open_id,
                        AvatarUrl = UC.GetAvatarUrl(authorizeResult.data.open_id)
                    };

                    await UserManager.CreateAsync(user, login.Password);
                }

                var roles = await UserManager.GetRolesAsync(user);

                if (authorizeResult.data.is_root)
                {
                    if (!roles.Any(x => x == "Root"))
                        await UserManager.AddToRoleAsync(user, "Root");
                }
                else
                {
                    if (roles.Any(x => x == "Root"))
                        await UserManager.RemoveFromRoleAsync(user, "Root");
                }

                await SignInManager.SignInAsync(user, true);
                user.LastLoginTime = DateTime.UtcNow;
                DB.SaveChanges();

                var cookie = HttpContext.Response.Headers["Set-Cookie"].ToString();
                var expire = DateTime.Parse(CookieExpireRegex.Match(cookie).Value).ToTimeStamp();

                return Result<dynamic>(new
                {
                    Cookie = cookie
                        .Replace(" httponly", "")
                        .Replace("samesite=lax", "")
                        .Replace("path=/;", ""),
                    Expire = expire
                });
            }
            else
            {
                return Result(400, authorizeResult.msg);
            }
        }

        [HttpDelete("session")]
        public async Task<IActionResult> DeleteSession() {
            await SignInManager.SignOutAsync();
            return Result(200, "Signed out");
        }
        #endregion

        #region Message
        [HttpGet("message")]
        public async Task<IActionResult> Message([FromServices] JoyOIUC UC, CancellationToken token)
        {
            if (User.IsSignedIn())
                return Result(await UC.HasUnreadMessageAsync(User.Current.OpenId));
            return Result(false);
        }
        #endregion

        #region Blog
        [HttpGet("{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}/blog")]
        public async Task<IActionResult> GetBlogUrl(
            [FromServices] ExternalApi XApi,
            string username,
            CancellationToken token
        )
        {
            return Result(await XApi.GetUserBlogDomainAsync(username, token));
        }

        [HttpGet("{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}/blog/posts")]
        public async Task<IActionResult> GetBlogPosts(
            [FromServices] ExternalApi XApi,
            string username,
            CancellationToken token
        )
        {
            return Json(await XApi.GetUserResolutionsAsync(username, 1, token));
        }
        #endregion

        #region Uploaded Problems
        [HttpGet("{username:regex(^[[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]]{{4,128}}$)}/uploadedproblem")]
        public async Task<IActionResult> GetUploadedProblem(string username, CancellationToken token)
        {
            var user = await User.Manager.FindByNameAsync(username);
            var problems = DB.Problems
                .Where(x => x.IsVisible)
                .Where(x => DB.UserClaims.Any(y => y.UserId == user.Id && y.ClaimType == Constants.ProblemEditPermission && y.ClaimValue == x.Id))
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    id = x.Id,
                    title = x.Title
                });

            return Result(problems);
        }
        #endregion
    }
}
