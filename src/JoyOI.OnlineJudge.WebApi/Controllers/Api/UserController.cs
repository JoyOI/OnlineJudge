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
using JoyOI.UserCenter.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private static Regex CookieExpireRegex = new Regex("(?<=; expires=)[0-9a-zA-Z: -/]{1,}(?=; path=)");

        #region User
        [HttpGet("all")]
        public async Task<ApiResult<PagedResult<IEnumerable<User>>>> Get(
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

            var result = await Paged(ret, page ?? 1, 50, token);
            var type = typeof(IdentityUser<Guid>);
            foreach (var x in result.data.result)
                foreach (var y in type.GetProperties().Where(y => y.Name != nameof(IdentityUser.Id) && y.Name != nameof(IdentityUser.UserName)))
                    y.SetValue(x, y.PropertyType.IsValueType ? Activator.CreateInstance(y.PropertyType) : null);

            return result;
        }

        [HttpGet("role")]
        public async Task<ApiResult<ConcurrentDictionary<string, object>>> GetUserRoles(string usernames, string userids, CancellationToken token)
        {
            var dic = new ConcurrentDictionary<string, object>();
            var roles = await DB.Roles.ToDictionaryAsync(x => x.Id, x => x.Name, token);

            if (!string.IsNullOrWhiteSpace(usernames))
            {
                var users = usernames.Split(',').Select(x => x.Trim());
                var tasks = new List<Task>();
                foreach (var x in users)
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        var user = DB.Users.SingleOrDefault(y => y.UserName == x);
                        var userId = user.Id;
                        var role = DB.UserRoles.FirstOrDefault(y => y.UserId == userId);
                        if (role == null)
                        {
                            dic.TryAdd(x, null);
                        }
                        else
                        {
                            dic.TryAdd(x, new { role = roles[role.RoleId], avatarUrl = user.AvatarUrl, username = user.UserName, id = user.Id });
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }
            else if (!string.IsNullOrWhiteSpace(userids))
            {
                var ids = userids.Split(',').Select(x => Guid.Parse(x.Trim()));
                var tasks = new List<Task>();
                foreach (var x in ids)
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        var user = DB.Users.SingleOrDefault(y => y.Id == x);
                        var username = user.UserName;
                        var role = DB.UserRoles.FirstOrDefault(y => y.UserId == x);
                        if (role == null)
                        {
                            dic.TryAdd(x.ToString(), new { username = username, role = (string)null, id = user.Id, avatarUrl = user.AvatarUrl });
                        }
                        else
                        {
                            dic.TryAdd(x.ToString(), new { username = username, role = roles[role.RoleId], avatarUrl = user.AvatarUrl, id = user.Id });
                        }
                    }));
                }
            }

            return Result(dic);
        }
        #endregion

        #region Session
        [HttpGet("session/info")]
        public async Task<ApiResult<dynamic>> GetSessionInfo(CancellationToken token)
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
            }
            return Result<dynamic>(ret);
        }

        [HttpPut("session")]
        public async Task<ApiResult<dynamic>> PutSession([FromServices] JoyOIUC UC, [FromBody] Login login, CancellationToken token)
        {
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
                user.LastLoginTime = DateTime.Now;
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
        #endregion
    }
}
