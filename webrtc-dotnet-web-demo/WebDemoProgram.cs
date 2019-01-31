using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
                .UseSetting("https_port", "5000")
                .UseUrls("https://0.0.0.0:5000")
                .UseStartup<Startup>();
    }
}
