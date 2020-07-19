using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using HandyHansel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HandyHansel
{
    /// <summary>
    /// An internal class used to generate the configuration information used for the myriad of different tools needed in the bot.
    /// </summary>
    internal class Config
    {
        internal Config()
        {
            ClientConfig = new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
            };

            IServiceProvider deps = new ServiceCollection()
                                        .AddDbContext<PostgreSqlContext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQL_CONN_STRING")))
                                        .AddScoped<IDataAccessProvider, DataAccessPostgreSqlProvider>()
                                        .BuildServiceProvider();

            CommandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { "^" },
                Services = deps,
                EnableDms = true,
            };

            InteractivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2),
            };
        }

        internal DiscordConfiguration ClientConfig { get; set; }
        internal CommandsNextConfiguration CommandsConfig { get; set; }
        internal InteractivityConfiguration InteractivityConfig { get; set; }
    }
}
