using HandyHansel.Models;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using Serilog.Formatting.Json;
using System;

namespace HandyHansel
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                           .Build()
                           .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                           .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                               .Enrich.FromLogContext()
                               .WriteTo.File(new JsonFormatter(renderMessage: true), "log.txt")
                               .WriteTo.Console()
                               .MinimumLevel.Debug()
                           )
                           .ConfigureHostConfiguration(ConfigureHostConfiguration(args))
                           .ConfigureServices(ConfigureServices);
        }

        public static Action<IConfigurationBuilder> ConfigureHostConfiguration(string[] args)
        {
            return configuration => configuration.AddJsonFile("config.json", optional: false)
                .AddCommandLine(args);
        }

        public static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.AddOptions<BotConfig>()
                .Bind(ctx.Configuration.GetSection(BotConfig.Section))
                .ValidateDataAnnotations();

            HangfireConfig hangfireConfig = new HangfireConfig();
            ctx.Configuration.GetSection(HangfireConfig.Section).Bind(hangfireConfig);

            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = hangfireConfig.Database.Host,
                Port = hangfireConfig.Database.Port,
                Database = hangfireConfig.Database.Name,
                Username = hangfireConfig.Database.Username,
                Password = hangfireConfig.Database.Password,
                Pooling = hangfireConfig.Database.Pooling,
            };

            _ = services
                .AddSingleton<IBotAccessProviderBuilder, BotAccessProviderBuilder>()
                .AddSingleton<BotService>()
                .AddHangfire(configuration => configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UsePostgreSqlStorage(connectionStringBuilder.ConnectionString, new PostgreSqlStorageOptions { DistributedLockTimeout = TimeSpan.FromMinutes(1), }))
                .AddHangfireServer(opts =>
                {
                    opts.StopTimeout = TimeSpan.FromSeconds(15);
                    opts.ShutdownTimeout = TimeSpan.FromSeconds(30);
                    opts.WorkerCount = 4;
                })
                .AddHostedService<NormHostedService>();
        }
    }
}