using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ElectronNET.API;

namespace JoyOI.OnlineJudge.Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseElectron(args)
                .UseUrls("http://*:5001")
                .Build();
    }
}
