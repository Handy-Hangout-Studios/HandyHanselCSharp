using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.VoiceNext;
using HandyHansel.Commands;
using HandyHansel.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HandyHansel
{
    public class BotService
    {
        private readonly ulong _devUserId;

        private readonly DiscordShardedClient _discord;
        private readonly CommandsNextConfiguration _commandsConfig;
        private readonly InteractivityConfiguration _interactivityConfig;
        private IReadOnlyDictionary<int, CommandsNextExtension> _commands;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "This dictionary is required for the interactivity extension even though it appears to be unused.")]
        private IReadOnlyDictionary<int, InteractivityExtension> _interactivity;
        private readonly IBotAccessProviderBuilder _accessBuilder;
        private readonly ILogger _logger;

        public readonly Dictionary<string, TimeZoneInfo> SystemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToDictionary(tz => tz.Id);
        public Dictionary<ulong, Dictionary<int, GuildBackgroundJob>> guildBackgroundJobs = new Dictionary<ulong, Dictionary<int, GuildBackgroundJob>>();

        private DiscordEmoji _clock;
        private Parser _timeParser;

        public BotService(ILoggerFactory loggerFactory, IOptions<BotConfig> botConfig, IBotAccessProviderBuilder dataAccessProviderBuilder, IServiceProvider services)
        {
            DiscordConfiguration ClientConfig = new DiscordConfiguration
            {
                Token = botConfig.Value.BotToken,
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Information,
                LoggerFactory = loggerFactory,
            };

            this._commandsConfig = new CommandsNextConfiguration
            {
                PrefixResolver = PrefixResolver,
                Services = services,
                EnableDms = true,
                EnableMentionPrefix = true,
            };

            this._interactivityConfig = new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromMinutes(2),
            };


            this._discord = new DiscordShardedClient(ClientConfig);
            this._devUserId = botConfig.Value.DevId;
            this._logger = loggerFactory.CreateLogger("BotService");
            this._accessBuilder = dataAccessProviderBuilder;
        }

        public async Task StartAsync()
        {
            this._commands = await this._discord.UseCommandsNextAsync(this._commandsConfig);
            this._interactivity = await this._discord.UseInteractivityAsync(this._interactivityConfig);
            IReadOnlyDictionary<int, VoiceNextExtension> test = await this._discord.UseVoiceNextAsync(new VoiceNextConfiguration());

            foreach (KeyValuePair<int, CommandsNextExtension> pair in this._commands)
            {
                pair.Value.RegisterCommands<GeneralCommands>();
                pair.Value.RegisterCommands<TimeCommands>();
                pair.Value.RegisterCommands<EventCommands>();
                pair.Value.RegisterCommands<PrefixCommands>();
                pair.Value.CommandErrored += this.LogExceptions;
            }

            this._discord.MessageCreated += this.CheckForDate;
            this._discord.MessageReactionAdded += this.SendAdjustedDate;
            this._discord.Ready += this.UpdateDiscordStatus;

            await this._discord.StartAsync();
            this._clock = DiscordEmoji.FromName(this._discord.ShardClients[0], ":clock:");
            this._timeParser = new Parser(this._logger, Parser.ParserType.Time);
        }

        private async Task UpdateDiscordStatus(DiscordClient sender, ReadyEventArgs e)
        {
            await this._discord.UpdateStatusAsync(new DiscordActivity("all the users in disappointment", ActivityType.Watching));
        }

        public async Task StopAsync()
        {
            await this._discord.StopAsync();
        }

        private async Task LogExceptions(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            await e.Context.Channel.SendMessageAsync("An error occurred");
            try
            {
                DiscordEmbed commandErrorEmbed = new DiscordEmbedBuilder()
                    .AddField("Message", e.Exception.Message)
                    .AddField("StackTrace", e.Exception.StackTrace);
                await e.Context.Guild.Members[this._devUserId].SendMessageAsync(embed: commandErrorEmbed);
                this._logger.LogError(e.Exception, "Exception from Command Errored");
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "An error occurred in sending the exception to the Dev");
            }
        }

        private async Task CheckForDate(DiscordClient c, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot)
            {
                return;
            }

            IEnumerable<Tuple<string, DateTime>> parserList = this._timeParser.DateTimeV2Parse(e.Message.Content);

            if (parserList.Count() != 0)
            {
                await e.Message.CreateReactionAsync(this._clock);
            }
        }

        private async Task SendAdjustedDate(DiscordClient c, MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot)
            {
                return;
            }

            DiscordChannel channel = await c.GetChannelAsync(e.Channel.Id);
            _ = Task.Run(async () =>
            {
                if (e.Emoji.Equals(this._clock))
                {
                    using IBotAccessProvider database = this._accessBuilder.Build();
                    DiscordMember reactor = (DiscordMember)e.User;
                    DiscordMessage msg = await channel.GetMessageAsync(e.Message.Id);
                    IEnumerable<Tuple<string, DateTime>> parserList = this._timeParser.DateTimeV2Parse(msg.Content);

                    if (!parserList.Any())
                    {
                        await e.Channel.SendMessageAsync("Hey, you're stupid, stop trying to react to messages that don't have times in them.");
                        return;
                    }

                    DiscordEmbedBuilder reactorTimeEmbed = new DiscordEmbedBuilder()
                                .WithTitle("You requested a timezone conversion");

                    try
                    {
                        foreach ((string parsedText, DateTime parsedTime) in parserList.Where(element => element.Item2 > DateTime.Now))
                        {
                            string opTimeZoneId = database.GetUsersTimeZone(msg.Author.Id)?.TimeZoneId;
                            string reactorTimeZoneId = database.GetUsersTimeZone(e.User.Id)?.TimeZoneId;

                            if (opTimeZoneId is null)
                            {
                                await channel.SendMessageAsync("The original poster has not set up a time zone yet.");
                                return;
                            }

                            if (reactorTimeZoneId is null)
                            {
                                await channel.SendMessageAsync("You have not set up a time zone yet.");
                                return;
                            }

                            if (!this.SystemTimeZones.ContainsKey(opTimeZoneId) || !this.SystemTimeZones.ContainsKey(reactorTimeZoneId))
                            {
                                await channel.SendMessageAsync(
                                    "There was a problem, please reach out to your bot developer.");
                                return;
                            }

                            TimeZoneInfo opTimeZone = this.SystemTimeZones[opTimeZoneId];
                            TimeZoneInfo reactorTimeZone = this.SystemTimeZones[reactorTimeZoneId];
                            DateTime reactorsTime = TimeZoneInfo.ConvertTime(parsedTime, opTimeZone, reactorTimeZone);
                            reactorTimeEmbed
                                .AddField("Poster's Time", $"\"{parsedText}\"")
                                .AddField("Your time", $"{reactorsTime:t}");
                        }
                        await reactor.SendMessageAsync(embed: reactorTimeEmbed);
                    }
                    catch (Exception exception)
                    {
                        this._logger.Log(LogLevel.Error, exception, "Error in sending reactor the DM");
                    }
                }
            });
        }

        public async Task SendEmbedWithMessageToChannelAsUser(CancellationToken token, ulong guildId, ulong userId, ulong channelId, string message, string title, string description)
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                DiscordClient shardClient = this._discord.GetShard(guildId);
                DiscordChannel channel = await shardClient.GetChannelAsync(channelId);
                DiscordUser poster = await shardClient.GetUserAsync(userId);
                this._discord.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                        .WithTitle(title)
                        .WithAuthor(poster.Username, iconUrl: poster.AvatarUrl)
                        .WithDescription(description)
                        .Build();
                await shardClient.SendMessageAsync(channel, message);
                await shardClient.SendMessageAsync(channel, embed: embed);
            }
            catch (Exception e)
            {
                this._logger.LogError(e, "Error in Sending Embed", guildId, userId, message, title, description);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task RemoveGuildBackgroundJob(ulong guildId, int jobId)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    this.guildBackgroundJobs[guildId][jobId].CancellationTokenSource.Cancel();
                    this.guildBackgroundJobs[guildId][jobId].CancellationTokenSource.Dispose();
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, "Error in removing guild background job");
                }
                this.guildBackgroundJobs[guildId].Remove(jobId);
            });
        }

        private async Task<int> PrefixResolver(DiscordMessage msg)

        {
            using IBotAccessProvider dataAccessProvider = this._accessBuilder.Build();
            List<GuildPrefix> guildPrefixes = dataAccessProvider.GetAllAssociatedGuildPrefixes(msg.Channel.GuildId).ToList();

            if (!guildPrefixes.Any())
            {
                return msg.GetStringPrefixLength("^");
            }

            foreach (int length in guildPrefixes.Select(prefix => msg.GetStringPrefixLength(prefix.Prefix)).Where(length => length != -1))
            {
                return length;
            }

            return -1;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
