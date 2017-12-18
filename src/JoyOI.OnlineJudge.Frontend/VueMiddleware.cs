using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace JoyOI.OnlineJudge.Frontend
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class VueMiddleware
    {
        private readonly RequestDelegate _next;

        public VueMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.HasValue && httpContext.Request.Path.Value == "/badbrowser")
            {
                return httpContext.Response.WriteAsync(File.ReadAllText(Path.Combine("wwwroot", "views", "badbrowser.html")));
            }
            else
            {
                return httpContext.Response.WriteAsync(File.ReadAllText(Path.Combine("wwwroot", "views", "index.html")));
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class VueMiddlewareExtensions
    {
        public static IApplicationBuilder UseVueMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<VueMiddleware>();
        }
    }
}
