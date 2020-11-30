using HandyHansel.Database.TypeHandlers;
using HandyHansel.Models;
using HandyHansel.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;
using NodaTime.TimeZones;
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

            using IStorageConnection connection = JobStorage.Current.GetConnection();
            foreach (RecurringJobDto rJob in StorageConnectionExtensions.GetRecurringJobs(connection))
            {
                RecurringJob.RemoveIfExists(rJob.Id);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                           .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
                               .Enrich.FromLogContext()
                               .WriteTo.File(formatter: new JsonFormatter(renderMessage: true),
                                             "log.txt")
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
                .AddScoped<IClock>((p) => SystemClock.Instance)
                .AddScoped<IDateTimeZoneSource>((p) => TzdbDateTimeZoneSource.Default)
                .AddScoped<IDateTimeZoneProvider, DateTimeZoneCache>()
                .AddScoped<IBotAccessProviderBuilder, BotAccessProviderBuilder>()
                .AddSingleton<BotService>()
                .AddScoped<EventService>()
                .AddScoped<ModerationService>()
                .AddHostedService<NormHostedService>()
                .AddHangfire(configuration =>
                {
                    configuration
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UsePostgreSqlStorage(connectionStringBuilder.ConnectionString, new PostgreSqlStorageOptions { DistributedLockTimeout = TimeSpan.FromMinutes(1), });
                    Dapper.SqlMapper.AddTypeHandler(new DapperDateTimeTypeHandler());
                })
                .AddHangfireServer(opts =>
                {
                    opts.StopTimeout = TimeSpan.FromSeconds(15);
                    opts.ShutdownTimeout = TimeSpan.FromSeconds(30);
                    opts.WorkerCount = 4;
                });
        }
    }
}