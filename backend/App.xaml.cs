using System.Windows;
using DotNetEnv; // Add this using statement
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting; // Make sure this is included for IWebHostBuilder

namespace backend
{
    public partial class App : Application
    {
        private IHost _host;

        // This method runs once when your application first starts.
        protected override void OnStartup(StartupEventArgs e)
        {
            // Load environment variables from the .env file
            Env.Load();
            
            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        options.ListenAnyIP(5000); // HTTP
                        options.ListenAnyIP(5001, listenOptions => // HTTPS
                        {
                            listenOptions.UseHttps();
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                })
                .Build();

            _host.Start(); // Start the host in the background

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                await _host.StopAsync(); // Stop the host gracefully on application exit
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}