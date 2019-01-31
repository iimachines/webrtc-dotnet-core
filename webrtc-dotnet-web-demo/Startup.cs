using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WonderMediaProductions.WebRtc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime lifetime)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            loggerFactory.AddDebug(LogLevel.Debug);

            app.UseHttpsRedirection();
            app.UseDeveloperExceptionPage();
            app.UseWebSockets();
            app.UseFileServer();

            lifetime.ApplicationStopping.Register(() => Console.WriteLine("Application stopping"));
            lifetime.ApplicationStopped.Register(() => Console.WriteLine("Application stopped"));

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/signaling")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        try
                        {
                            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            await RtcServer.Run(webSocket, lifetime.ApplicationStopping);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });
        }
    }
}
