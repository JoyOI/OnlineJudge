using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;

namespace JoyOI.OnlineJudge.WebApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration(out IConfiguration config);
            services.AddEntityFrameworkMySql()
                .AddDbContextPool<OnlineJudgeContext>(x => 
                {
                    x.UseMySql(config["Data:MySQL"]);
                    x.UseMySqlLolita();
                });

            services.AddJoyOIManagementService();
            services.AddJoyOIUserCenter();

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

            services.AddCors(c => c.AddPolicy("OnlineJudge", x =>
                x.AllowCredentials()
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
            ));
        }
        
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseCors("OnlineJudge");
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using (var db = serviceScope.ServiceProvider.GetService<OnlineJudgeContext>())
            {
                await db.InitializeAsync();
            }
        }
    }
}
