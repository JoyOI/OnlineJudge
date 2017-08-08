using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoyOI.UserCenter.SDK;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Models;

namespace JoyOI.OnlineJudge.WebApi.Controllers.Api
{
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private static Regex CookieExpireRegex = new Regex("(?<=; expires=)[0-9a-zA-Z: -/]{1,}(?=; path=)");

        [HttpGet("session/info")]
        public ApiResult<dynamic> GetSessionInfo()
        {
            var ret = new Dictionary<string, object>();
            ret.Add("isSignedIn", User.Current != null);
            if (User.Current != null)
            {
                ret.Add("username", User.Current.UserName);
                ret.Add("email", User.Current.Email);
                ret.Add("nickname", User.Current.Nickname);
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
                cookie = cookie.Substring(0, cookie.IndexOf("; path="));

                return Result<dynamic>(new { Cookie = cookie, Expire = expire });
            }
            else
            {
                return Result(400, authorizeResult.msg);
            }
        }
    }
}
