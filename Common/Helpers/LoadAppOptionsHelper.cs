using Common.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Common.Helpers
{
    public static class LoadAppOptionsHelper
    {
        /// <summary>
        /// Load AppOptions from Common/appsettings.json
        /// </summary>
        public static AppOptions LoadAppOptions(WebApplicationBuilder builder)
        {
            // Find the configuration file
            var basePath = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(basePath, "../Common/appsettings.json");
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                basePath = AppContext.BaseDirectory;
                configPath = Path.Combine(basePath, "appsettings.json");
            }
            

            if (!File.Exists(configPath))
            {
                var errorMsg = $"Configuration file not found: {configPath}";
                Log.Fatal(errorMsg);
                throw new FileNotFoundException(errorMsg);
            }

            // Add configuration sources
            builder.Configuration
                .SetBasePath(Path.GetDirectoryName(configPath)!)
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs());

            // Bind AppOptions
            var appOptions = builder.Configuration.GetSection("AppOptions").Get<AppOptions>();
            if (appOptions == null)
            {
                var errorMsg = "Missing AppOptions configuration in Common/appsettings.json";
                Log.Fatal(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Register AppOptions in DI
            builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("AppOptions"));
            return appOptions;
        }

        public static AppOptions LoadAppOptions(IHostBuilder builder)
        {
            // Find the configuration file
            var basePath = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(basePath, "../Common/appsettings.json");

            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                basePath = AppContext.BaseDirectory;
                configPath = Path.Combine(basePath, "appsettings.json");
            }


            if (!File.Exists(configPath))
            {
                var errorMsg = $"Configuration file not found: {configPath}";
                Log.Fatal(errorMsg);
                throw new FileNotFoundException(errorMsg);
            }

            // Create a new configuration builder
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(configPath)!)
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .Build();

            // Bind AppOptions
            var appOptions = configuration.GetSection("AppOptions").Get<AppOptions>();
            if (appOptions == null)
            {
                var errorMsg = "Missing AppOptions configuration in Common/appsettings.json";
                Log.Fatal(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Add AppOptions to DI
            builder.ConfigureServices((hostContext, services) =>
            {
                services.Configure<AppOptions>(configuration.GetSection("AppOptions"));
            });

            return appOptions;
        }
    }
}