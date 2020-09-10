using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using HandyHansel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

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
                MinimumLogLevel = LogLevel.Information,
            };

            IServiceProvider deps = new ServiceCollection()
                                        .AddDbContext<PostgreSqlContext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQL_CONN_STRING")!))
                                        .AddScoped<IDataAccessProvider, DataAccessPostgreSqlProvider>()
                                        .BuildServiceProvider();

            CommandsConfig = new CommandsNextConfiguration
            {
                PrefixResolver = PrefixResolver,
                Services = deps,
                EnableDms = true,
                EnableMentionPrefix = true,
            };

            InteractivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2),
            };
        }
        

#pragma warning disable 1998
        private static async Task<int> PrefixResolver(DiscordMessage msg)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            List<GuildPrefix> guildPrefixes =
                dataAccessProvider.GetAllAssociatedGuildPrefixes(msg.Channel.GuildId).ToList();
            if (!guildPrefixes.Any())
                return msg.GetStringPrefixLength("^");
            foreach (int length in guildPrefixes.Select(prefix => msg.GetStringPrefixLength(prefix.Prefix)).Where(length => length != -1))
            {
                return length;
            }

            return -1;
        }
#pragma warning restore 1998

        internal DiscordConfiguration ClientConfig { get; }
        internal CommandsNextConfiguration CommandsConfig { get; }
        internal InteractivityConfiguration InteractivityConfig { get; }
    }
}
