using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<OnlineJudgeContext>(x => 
            {
                x.UseMySql("server=localhost;uid=root;pwd=123456;database=joyoi_oj");
                x.UseMySqlLolita();
            });

            services.AddJoyOIManagementService();

            services.AddIdentity<User, IdentityRole<Guid>>(x =>
            {
                x.Password.RequireDigit = false;
                x.Password.RequiredLength = 0;
                x.Password.RequireLowercase = false;
                x.Password.RequireNonAlphanumeric = false;
                x.Password.RequireUppercase = false;
                x.User.AllowedUserNameCharacters = null;
            })
                .AddEntityFrameworkStores<OnlineJudgeContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
        }
    }
}
