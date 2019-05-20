using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace WonderMediaProductions.WebRtc
{
    public class WebDemoProgram
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSetting("https_port", "8080")
                .UseUrls("https://0.0.0.0:8080")
                .UseStartup<Startup>()
                .ConfigureLogging((hostingContext, logging) => logging.AddConsole());
    }
}
