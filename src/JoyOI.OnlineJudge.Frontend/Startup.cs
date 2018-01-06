using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace JoyOI.OnlineJudge.Frontend
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(c => c.AddPolicy("OnlineJudge", x =>
                x.AllowCredentials()
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
            ));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors("OnlineJudge");
            app.UseStaticFiles();
            app.UseDeveloperExceptionPage();
            app.UseVueMiddleware();
            if (HybridSupport.IsElectronActive)
            {
                ElectronBootstrap();
            }
        }

        public async void ElectronBootstrap()
        {
            var browserWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                Width = 1280,
                Height = 864,
                Show = false,
            }, "http://localhost:5001");

            browserWindow.OnReadyToShow += () => browserWindow.Show();
            browserWindow.SetTitle("Electron.NET API Demos");
        }
    }
}
