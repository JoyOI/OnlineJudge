using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Swagger;
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
                .AddDbContext<OnlineJudgeContext>(x => 
                {
                    x.UseMySql(config["Data:MySQL"]);
                    x.UseMySqlLolita();
                });

            var redisOptions = new ConfigurationOptions();
            redisOptions.EndPoints.Add(config["Data:Redis:Host"]);
            redisOptions.Password = config["Data:Redis:Password"];
            redisOptions.AbortOnConnectFail = false;
            redisOptions.Ssl = false;
            redisOptions.ResponseTimeout = 100000;
            redisOptions.ChannelPrefix = "SHARED_KEYS";
            redisOptions.DefaultDatabase = 5;
            var redis = ConnectionMultiplexer.Connect(redisOptions);

            services.AddDataProtection()
                .PersistKeysToRedis(redis, "ASPNET_DATA_PROTECTION_KEYS");

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

            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("api", new Info() { Title = "JoyOI Online Judge" });
                x.IncludeXmlComments((Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "JoyOI.OnlineJudge.WebApi.xml")));
            });
        }
        
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseCors("OnlineJudge");
            app.UseCookieMiddleware();
            app.UseAuthentication();
            app.UseErrorHandlingMiddleware();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
                c.SwaggerEndpoint("/swagger/swagger.json", "JoyOI Online Judge"));

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
