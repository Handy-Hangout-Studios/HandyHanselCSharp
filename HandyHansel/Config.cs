using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using HandyHansel.Models;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;

namespace HandyHansel
{
    /// <summary>
    ///     An internal class used to generate the configuration information used for the myriad of different tools needed in
    ///     the bot.
    /// </summary>
    internal class Config
    {
        internal Config()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("config.json")
                    .Build();
            JsonConfig = new ConfigJson();
            config.Bind(JsonConfig);

            ClientConfig = new DiscordConfiguration
            {
                Token = JsonConfig.BotToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
            };
            
            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = JsonConfig.Host,
                Port = JsonConfig.Port,
                Database = JsonConfig.BotDatabase,
                Username = JsonConfig.Username,
                Password = JsonConfig.Password,
                Pooling = JsonConfig.Pooling,
            };

            IServiceProvider deps = new ServiceCollection()
                .AddSingleton(typeof(IDataAccessProviderBuilder), new DataAccessProviderBuilder(connectionStringBuilder.ConnectionString))
                .BuildServiceProvider();
            DataProviderBuilder = deps.GetService<IDataAccessProviderBuilder>();
            
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

            DevUserId = JsonConfig.DevId;

            connectionStringBuilder.Database = JsonConfig.HangfireDatabase;
            GlobalConfiguration.Configuration.UsePostgreSqlStorage(connectionStringBuilder.ConnectionString);
        }

        internal DiscordConfiguration ClientConfig { get; }
        internal CommandsNextConfiguration CommandsConfig { get; }
        internal ulong DevUserId { get; }
        internal InteractivityConfiguration InteractivityConfig { get; }
        private ConfigJson JsonConfig { get; }
        
        private IDataAccessProviderBuilder DataProviderBuilder { get; }


#pragma warning disable 1998
        private async Task<int> PrefixResolver(DiscordMessage msg)
        {
            using IDataAccessProvider dataAccessProvider = DataProviderBuilder.Build();
            List<GuildPrefix> guildPrefixes =
                dataAccessProvider.GetAllAssociatedGuildPrefixes(msg.Channel.GuildId).ToList();
            if (!guildPrefixes.Any())
                return msg.GetStringPrefixLength("^");
            foreach (int length in guildPrefixes.Select(prefix => msg.GetStringPrefixLength(prefix.Prefix))
                .Where(length => length != -1)) return length;

            return -1;
        }
#pragma warning restore 1998
    }
}