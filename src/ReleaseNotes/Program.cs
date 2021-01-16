using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace ReleaseNotes
{
    internal class Program
    {
        public static IConfigurationRoot Configuration { get; private set; }

        private static async Task<int> Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string instrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
            var configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = instrumentationKey;

            Log.Logger = new LoggerConfiguration()
                   .ReadFrom.Configuration(Configuration)
                   .WriteTo
                        .ApplicationInsights(new TelemetryClient(configuration), TelemetryConverter.Traces)
                   .CreateLogger();

            try
            {
                Log.Debug("app starting");
                return await CreateHostBuilder().RunCommandLineApplicationAsync<ReleaseNotesCmd>(args).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex.Message);
                return 1;
            }
            finally
            {
                Log.Debug("app terminated");
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<IReleaseNotesService, ReleaseNotesService>();
                    services.AddLogging(config =>
                    {
                        config.ClearProviders();
                        config.AddProvider(new SerilogLoggerProvider(Log.Logger));
                    });
                    services.Configure<AppOptions>(Configuration.GetSection(AppOptions.SectionName));
                });
        }
    }
}
