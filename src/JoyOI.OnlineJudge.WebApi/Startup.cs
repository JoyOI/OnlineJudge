using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JoyOI.OnlineJudge.Models;
using JoyOI.OnlineJudge.WebApi.Lib;
using JoyOI.OnlineJudge.WebApi.Hubs;

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
			services.AddStateMachineAwaiter();
            services.AddJoyOIUserCenter();

            services.AddIdentity<User, Role>(x =>
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

            services.AddSmartCookies();
            services.AddSmartUser<User, Guid>();
            services.AddMvc();
            services.AddSignalR()
                .AddRedis(x => 
                {
                    x.Options.EndPoints.Add(config["Data:Redis:Host"]);
                    x.Options.Password = config["Data:Redis:Password"];
                    x.Options.AbortOnConnectFail = false;
                    x.Options.Ssl = false;
                    x.Options.ResponseTimeout = 100000;
                    x.Options.ChannelPrefix = "ONLINE_JUDGE";
                    x.Options.DefaultDatabase = 2;
                });

            services.AddCors(c => c.AddPolicy("OnlineJudge", x =>
                x.AllowCredentials()
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
            ));

            services.AddJudgeStateMachineHandler();
            services.AddExternalApi();
        }
        
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseCors("OnlineJudge");
            app.UseCookieMiddleware();
            app.UseAuthentication();
            app.UseErrorHandlingMiddleware();
            app.UseSignalR(x =>
            {
                x.MapHub<OnlineJudgeHub>("signalr/onlinejudge");
            });
            app.UseMvcWithDefaultRoute();

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using (var db = serviceScope.ServiceProvider.GetService<OnlineJudgeContext>())
            {
                await db.InitializeAsync();
            }
        }
    }
}
