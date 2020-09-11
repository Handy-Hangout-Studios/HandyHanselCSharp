using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using HandyHansel.Commands;
using HandyHansel.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace HandyHansel
{
    public static class Program
    {
        private static Timer _eventTimer;
        private static DiscordClient _discord;
        private static CommandsNextExtension _commands;
        // ReSharper disable once NotAccessedField.Local
        private static InteractivityExtension _interactivity;
        public static ILogger Logger;

        public static readonly Dictionary<string, TimeZoneInfo> SystemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToDictionary(tz => tz.Id);
        
        public static void Main()
        {    
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .Enrich.WithExceptionDetails()
                    .WriteTo.RollingFile(
                        new JsonFormatter(renderMessage: true),
                        "HandyHanselLog-{Date}.txt"
                    )
                    .CreateLogger();
                MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch(Exception exception)
            {
                Log.Logger.Error(exception, "Exception from Normal Running");
            }
        }

        private static async Task MainAsync()
        {
            Config config = new Config();
            _discord = new DiscordClient(config.ClientConfig);
            Logger = _discord.Logger;
            _commands = _discord.UseCommandsNext(config.CommandsConfig);
            _interactivity = _discord.UseInteractivity(config.InteractivityConfig);

            _commands.RegisterCommands<GeneralCommands>();
            _commands.RegisterCommands<TimeCommands>();
            _commands.RegisterCommands<EventCommands>();
            _commands.RegisterCommands<PrefixCommands>();

            _discord.MessageCreated += CheckForDate;
            _discord.MessageReactionAdded += SendAdjustedDate;
            _discord.Ready += StartTimer;
            _commands.CommandErrored += LogExceptions;

            await _discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task LogExceptions(CommandErrorEventArgs e)
        {
            ulong devUserId = Convert.ToUInt64(Environment.GetEnvironmentVariable("DEV_USER_ID"));
            DiscordEmbed commandErrorEmbed = new DiscordEmbedBuilder()
                .WithTitle(e.Command.QualifiedName)
                .AddField("Message", e.Exception.Message)
                .AddField("StackTrace", e.Exception.StackTrace);
            await e.Context.Guild.Members[devUserId].SendMessageAsync(embed: commandErrorEmbed);
            Log.Logger.Error(e.Exception, "Exception from Command Errored");
        }

        private static async Task StartTimer(ReadyEventArgs args)
        {
            SetTimer();
            _discord.Logger.Log(LogLevel.Information, "Timer", "Timer has been started", DateTime.Now);
            await Task.FromResult(0);
        }
        private static void SetTimer()
        {
            _eventTimer = new Timer(6000);
            _eventTimer.Elapsed += CheckForScheduledEvents;
            _eventTimer.AutoReset = true;
            _eventTimer.Enabled = true;
        }
    
        
        private static readonly HashSet<int> TenMinuteNotified = new HashSet<int>();
        private static async void CheckForScheduledEvents(object source, ElapsedEventArgs e)
        {
            using IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            _discord.Logger.Log(LogLevel.Information, "Timer", "Timer event fired", DateTime.Now);
            IEnumerable<ScheduledEvent> allScheduledEvents = database.GetAllPastScheduledEvents();
            foreach (ScheduledEvent se in allScheduledEvents)
            {
                lock (TenMinuteNotified)
                {
                    TenMinuteNotified.Remove(se.Id);
                }
                database.SetEventAnnounced(se);
                DiscordChannel channel = await _discord.GetChannelAsync(se.ChannelId);
                _discord.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}",
                    DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                    .WithTitle(se.Event.EventName)
                    .WithAuthor(_discord.CurrentUser.Username, iconUrl: _discord.CurrentUser.AvatarUrl)
                    .WithDescription(se.Event.EventDesc)
                    .Build();
                await _discord.SendMessageAsync(channel, "@everyone, this event is starting now!");
                await _discord.SendMessageAsync(channel, embed: embed);
            }

            database.SaveAnnouncedEvents();

            List<ScheduledEvent> allUpcomingScheduledEvents =
                database.GetAllPastScheduledEvents(TimeSpan.FromMinutes(10)).ToList();
            lock (TenMinuteNotified)
            {
                allUpcomingScheduledEvents = allUpcomingScheduledEvents.Where(scheduledEvent =>
                    !TenMinuteNotified.Contains(scheduledEvent.Id)).ToList();

                foreach (ScheduledEvent scheduledEvent in allUpcomingScheduledEvents)
                {
                    TenMinuteNotified.Add(scheduledEvent.Id);
                }
            }
            
            foreach (ScheduledEvent se in allUpcomingScheduledEvents)
            {
                DiscordChannel channel = await _discord.GetChannelAsync(se.ChannelId);
                _discord.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}",
                    DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                    .WithTitle(se.Event.EventName)
                    .WithAuthor(_discord.CurrentUser.Username, iconUrl: _discord.CurrentUser.AvatarUrl)
                    .WithDescription(se.Event.EventDesc)
                    .Build();
                await _discord.SendMessageAsync(channel, "@everyone, this event is starting in 10 minutes!");
                await _discord.SendMessageAsync(channel, embed: embed);
            }
        }

        private static DiscordEmoji _clock;
        private static DiscordEmoji SetClock()
        {
            return _clock ??= DiscordEmoji.FromName(_discord, ":clock:");
        }
        private static DiscordEmoji Clock => _clock ?? SetClock();
        
        private static Parser _timeParser;
        private static Parser SetTimeParser()
        {
            return _timeParser ??= new Parser(Parser.ParserType.Time);
        }
        private static Parser TimeParser => _timeParser ?? SetTimeParser();
        
        private static async Task CheckForDate(MessageCreateEventArgs e)
        {
            if (e.Author.IsBot) return;
            
            IEnumerable<Tuple<string, DateTime>> parserList = TimeParser.DateTimeV2Parse(e.Message.Content);

            if (parserList.Count() != 0)
            {
                await e.Message.CreateReactionAsync(Clock);
            }
        }
        private static async Task SendAdjustedDate(MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot) return;
            
            using IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            if (e.Emoji.Equals(Clock))
            {
                DiscordChannel channel = await _discord.GetChannelAsync(e.Channel.Id);
                DiscordMessage msg = await channel.GetMessageAsync(e.Message.Id);
                IEnumerable<Tuple<string, DateTime>> parserList = TimeParser.DateTimeV2Parse(msg.Content);
                foreach ((string parsedText, DateTime parsedTime) in parserList.Where(element => element.Item2 > DateTime.Now))
                {
                    DiscordMember reactor = (DiscordMember) e.User;
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
    }
}