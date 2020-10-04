using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using HandyHansel.Commands;
using HandyHansel.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HandyHansel
{
    public static class Program
    {
        private static readonly ulong _devUserId;
        private static readonly DiscordShardedClient _discord;
        private static IReadOnlyDictionary<int, CommandsNextExtension> _commands;
        private static readonly Config _config;

        // ReSharper disable once NotAccessedField.Local
        private static IReadOnlyDictionary<int, InteractivityExtension> _interactivity;
        public static Microsoft.Extensions.Logging.ILogger Logger;

        public static readonly Dictionary<string, TimeZoneInfo> SystemTimeZones =
            TimeZoneInfo.GetSystemTimeZones().ToDictionary(tz => tz.Id);

        public static Dictionary<ulong, List<GuildBackgroundJob> > guildBackgroundJobs =
            new Dictionary<ulong, List<GuildBackgroundJob> >();

        private static DiscordEmoji _clock;

        private static Parser _timeParser;
        private static DiscordEmoji Clock => _clock ?? SetClock();
        private static Parser TimeParser => _timeParser ?? SetTimeParser();

        static Program()
        {
            _config = new Config();
            _discord = new DiscordShardedClient(_config.ClientConfig);
            _devUserId = _config.DevUserId;
            Logger = _discord.Logger;
        }

        public static void Main()
        {
            MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            _commands = await _discord.UseCommandsNextAsync(_config.CommandsConfig);
            _interactivity = await _discord.UseInteractivityAsync(_config.InteractivityConfig);

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

            await _discord.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task LogExceptions(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            DiscordEmbed commandErrorEmbed = new DiscordEmbedBuilder()
                .AddField("Message", e.Exception.Message)
                .AddField("StackTrace", e.Exception.StackTrace);
            await e.Context.Guild.Members[_devUserId].SendMessageAsync(embed: commandErrorEmbed);
            Log.Logger.Error(e.Exception, "Exception from Command Errored");
        }

        private static DiscordEmoji SetClock()
        {
            return _clock ??= DiscordEmoji.FromName(_discord.ShardClients[0], ":clock:");
        }

        private static Parser SetTimeParser()
        {
            return _timeParser ??= new Parser(Parser.ParserType.Time);
        }

        private static async Task CheckForDate(DiscordClient c, MessageCreateEventArgs e)
        {
            if (e.Author.IsBot) return;

            IEnumerable<Tuple<string, DateTime>> parserList = TimeParser.DateTimeV2Parse(e.Message.Content);

            if (parserList.Count() != 0) await e.Message.CreateReactionAsync(Clock);
        }

        private static async Task SendAdjustedDate(DiscordClient c, MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot) return;

            using IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
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
                        Log.Logger.Error(exception, "Exception from Sending Adjusted Time to the Reactor");
                        Logger.Log(LogLevel.Error, exception, "Error in sending reactor the DM");
                    }
                }
            }
        }

        public static async void SendEmbedWithMessageToChannelAsUser(CancellationToken token, ulong guildId, ulong userId, ulong channelId, string message, string title, string description)
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
    }
}