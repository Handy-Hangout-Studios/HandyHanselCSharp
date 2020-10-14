﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using HandyHansel.Commands;
using HandyHansel.Models;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HandyHansel
{
    public class BotService
    {
        private readonly ulong _devUserId;

        private readonly DiscordShardedClient _discord;
        private readonly CommandsNextConfiguration _commandsConfig;
        private readonly InteractivityConfiguration _interactivityConfig;
        private IReadOnlyDictionary<int, CommandsNextExtension> _commands;
        private IReadOnlyDictionary<int, InteractivityExtension> _interactivity;
        private readonly IBotAccessProviderBuilder _accessBuilder;
        private readonly ILogger _logger;
        
        public readonly Dictionary<string, TimeZoneInfo> SystemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToDictionary(tz => tz.Id);
        public Dictionary<ulong, List<GuildBackgroundJob>> guildBackgroundJobs = new Dictionary<ulong, List<GuildBackgroundJob>>();

        private DiscordEmoji _clock;
        private Parser _timeParser;
        private DiscordEmoji Clock => _clock ?? SetClock();
        private Parser TimeParser => _timeParser ?? SetTimeParser();

        public BotService(ILoggerFactory loggerFactory, IOptions<BotConfig> botConfig, IBotAccessProviderBuilder dataAccessProviderBuilder, IServiceProvider services)
        {
            DiscordConfiguration ClientConfig = new DiscordConfiguration
            {
                Token = botConfig.Value.BotToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                LoggerFactory = loggerFactory,
            };

            _commandsConfig = new CommandsNextConfiguration
            {
                PrefixResolver = PrefixResolver,
                Services = services,
                EnableDms = true,
                EnableMentionPrefix = true,
            };

            _interactivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2),
            };


            _discord = new DiscordShardedClient(ClientConfig);
            _devUserId = botConfig.Value.DevId;
            _logger = loggerFactory.CreateLogger("BotService");
            _accessBuilder = dataAccessProviderBuilder;
        }

        public async Task StartAsync()
        {
            _commands = await _discord.UseCommandsNextAsync(_commandsConfig);
            _interactivity = await _discord.UseInteractivityAsync(_interactivityConfig);

            foreach (KeyValuePair<int, CommandsNextExtension> pair in _commands)
            {
                pair.Value.RegisterCommands<GeneralCommands>();
                pair.Value.RegisterCommands<TimeCommands>();
                pair.Value.RegisterCommands<EventCommands>();
                pair.Value.RegisterCommands<PrefixCommands>();
                pair.Value.CommandErrored += LogExceptions;
            }

            _discord.MessageCreated += CheckForDate;
            _discord.MessageReactionAdded += SendAdjustedDate;
            _discord.Ready += UpdateDiscordStatus;
            await _discord.StartAsync();
            
        }

        private async Task UpdateDiscordStatus(DiscordClient sender, ReadyEventArgs e)
        {
            await _discord.UpdateStatusAsync(new DiscordActivity("all the users in disappointment", ActivityType.Watching));
        }

        public async Task StopAsync()
        {
            await _discord.StopAsync();
        }

        private  async Task LogExceptions(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            DiscordEmbed commandErrorEmbed = new DiscordEmbedBuilder()
                .AddField("Message", e.Exception.Message)
                .AddField("StackTrace", e.Exception.StackTrace);
            await e.Context.Guild.Members[_devUserId].SendMessageAsync(embed: commandErrorEmbed);
            Serilog.Log.Logger.Error(e.Exception, "Exception from Command Errored");
        }

        private  DiscordEmoji SetClock()
        {
            return _clock ??= DiscordEmoji.FromName(_discord.ShardClients[0], ":clock:");
        }

        private  Parser SetTimeParser()
        {
            return _timeParser ??= new Parser(_logger, Parser.ParserType.Time);
        }

        private  async Task CheckForDate(DiscordClient c, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot) return;

            IEnumerable<Tuple<string, DateTime>> parserList = TimeParser.DateTimeV2Parse(e.Message.Content);

            if (parserList.Count() != 0) await e.Message.CreateReactionAsync(Clock);
        }

        private  async Task SendAdjustedDate(DiscordClient c, MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot) return;

            using IBotAccessProvider database = _accessBuilder.Build();
            if (e.Emoji.Equals(Clock))
            {
                DiscordChannel channel = await c.GetChannelAsync(e.Channel.Id);
                DiscordMessage msg = await channel.GetMessageAsync(e.Message.Id);
                IEnumerable<Tuple<string, DateTime>> parserList = TimeParser.DateTimeV2Parse(msg.Content);
                foreach ((string parsedText, DateTime parsedTime) in parserList.Where(element =>
                    element.Item2 > DateTime.Now))
                {
                    DiscordMember reactor = (DiscordMember)e.User;
                    string opTimeZoneId = database.GetUsersTimeZone(msg.Author.Id)?.TimeZoneId;
                    string reactorTimeZoneId = database.GetUsersTimeZone(e.User.Id)?.TimeZoneId;
                    if (opTimeZoneId is null)
                    {
                        await reactor.SendMessageAsync("The original poster has not set up a time zone yet.");
                        return;
                    }

                    if (reactorTimeZoneId is null)
                    {
                        await reactor.SendMessageAsync("You have not set up a time zone yet.");
                        return;
                    }

                    if (!SystemTimeZones.ContainsKey(opTimeZoneId) || !SystemTimeZones.ContainsKey(reactorTimeZoneId))
                    {
                        await reactor.SendMessageAsync(
                            "There was a problem, please reach out to your bot developer.");
                        return;
                    }

                    TimeZoneInfo opTimeZone = SystemTimeZones[opTimeZoneId];
                    TimeZoneInfo reactorTimeZone = SystemTimeZones[reactorTimeZoneId];
                    DateTime reactorsTime = TimeZoneInfo.ConvertTime(parsedTime, opTimeZone, reactorTimeZone);
                    DiscordEmbed reactorTimeEmbed = new DiscordEmbedBuilder()
                        .WithTitle("You requested a timezone conversion")
                        .AddField("Poster's Time", $"\"{parsedText}\"")
                        .AddField("Your time", $"{reactorsTime:t}");

                    try
                    {
                        await reactor.SendMessageAsync(embed: reactorTimeEmbed);
                    }
                    catch (Exception exception)
                    {
                        Serilog.Log.Logger.Error(exception, "Exception from Sending Adjusted Time to the Reactor");
                        _logger.Log(LogLevel.Error, exception, "Error in sending reactor the DM");
                    }
                }
            }
        }

        public  async Task SendEmbedWithMessageToChannelAsUser(CancellationToken token, ulong guildId, ulong userId, ulong channelId, string message, string title, string description)
        {
            if (token.IsCancellationRequested) return;
            DiscordClient shardClient = _discord.GetShard(guildId);
            DiscordChannel channel = await shardClient.GetChannelAsync(channelId);
            DiscordUser poster = await shardClient.GetUserAsync(userId);
            _discord.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
            DiscordEmbed embed = new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithAuthor(poster.Username, iconUrl: poster.AvatarUrl)
                    .WithDescription(description)
                    .Build();
            await shardClient.SendMessageAsync(channel, message);
            await shardClient.SendMessageAsync(channel, embed: embed);
        }

        private async Task<int> PrefixResolver(DiscordMessage msg)
        {
            using IBotAccessProvider dataAccessProvider = _accessBuilder.Build();
            List<GuildPrefix> guildPrefixes = dataAccessProvider.GetAllAssociatedGuildPrefixes(msg.Channel.GuildId).ToList();

            if (!guildPrefixes.Any()) return msg.GetStringPrefixLength("^");

            foreach (int length in guildPrefixes.Select(prefix => msg.GetStringPrefixLength(prefix.Prefix)).Where(length => length != -1))
            {
                return length;
            }

            return -1;
        }
    }
}
