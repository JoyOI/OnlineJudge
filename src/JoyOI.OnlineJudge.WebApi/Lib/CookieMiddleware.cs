using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace JoyOI.OnlineJudge.WebApi.Lib
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class CookieMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.ContainsKey("joyoi_cookie"))
            {
                httpContext.Request.Headers["Cookie"] = httpContext.Request.Headers["joyoi_cookie"];
            }

            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class CookieMiddlewareExtensions
    {
        public static IApplicationBuilder UseCookieMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CookieMiddleware>();
        }
    }
}
