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
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;

namespace HandyHansel
{
    public static class Program
    {
        private static Timer _eventTimer;
        private static DiscordClient _discord;
        private static CommandsNextExtension _commands;
        // ReSharper disable once NotAccessedField.Local
        private static InteractivityExtension _interactivity;

        public static readonly Dictionary<string, TimeZoneInfo> SystemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToDictionary(tz => tz.Id);
        
        public static void Main()
        {
            MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            Config config = new Config();
            _discord = new DiscordClient(config.ClientConfig);
            _commands = _discord.UseCommandsNext(config.CommandsConfig);
            _interactivity = _discord.UseInteractivity(config.InteractivityConfig);

            //commands.RegisterCommands<DNDCommands>(); // Is currently empty and so becomes NULL during the typeof used in the DSharpPlus.CommandsNext code
            _commands.RegisterCommands<GeneralCommands>();
            //commands.RegisterCommands<MinecraftCommands>(); // Is currently empty and so becomes NULL during the typeof used in the DSharpPlus.CommandsNext code
            _commands.RegisterCommands<TimeCommands>();
            _commands.RegisterCommands<EventCommands>();

            _discord.MessageCreated += CheckForDate;
            _discord.MessageReactionAdded += SendAdjustedDate;

            StartTimer();
            await _discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void StartTimer()
        {
            SetTimer();
            _discord.Logger.Log(LogLevel.Information, "Timer", "Timer has been started", DateTime.Now);
        }
        private static void SetTimer()
        {
            _eventTimer = new Timer(6000);
            _eventTimer.Elapsed += OnTimedEvent;
            _eventTimer.AutoReset = true;
            _eventTimer.Enabled = true;
        }

        private static async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _discord.Logger.Log(LogLevel.Information, "Timer", "Timer event fired", DateTime.Now);
            IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            IEnumerable<ScheduledEvent> allScheduledEvents = database.GetAllPastScheduledEvents();
            foreach (ScheduledEvent se in allScheduledEvents)
            {
                database.DeleteScheduledEvent(se);
                DiscordChannel channel = await _discord.GetChannelAsync(se.ChannelId);
                _discord.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                    .WithTitle(se.Event.EventName)
                    .WithAuthor(_discord.CurrentUser.Username, iconUrl: _discord.CurrentUser.AvatarUrl)
                    .WithDescription(se.Event.EventDesc)
                    .Build();
                
                await _discord.SendMessageAsync(channel, embed: embed);
            }
        }

        private static DiscordEmoji _clock;
        private static DiscordEmoji Clock()
        {
            return _clock ??= DiscordEmoji.FromName(_discord, ":clock:");
        }
        private static async Task CheckForDate(MessageCreateEventArgs e)
        {
            List<ModelResult> dateTimeList =
                DateTimeRecognizer.RecognizeDateTime(e.Message.Content, Culture.English);
            
            if (dateTimeList.Count > 0 &&
                dateTimeList.Any(elem => elem.TypeName.EndsWith("time") || elem.TypeName.EndsWith("date")))
            {
                await e.Message.CreateReactionAsync(Clock());
            }
        }
        private static async Task SendAdjustedDate(MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot) return;
            
            IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            if (e.Emoji.Equals(Clock()))
            {
                List<ModelResult> dateTimeList =
                    DateTimeRecognizer.RecognizeDateTime(e.Message.Content, Culture.English);
                foreach (ModelResult modelResult in dateTimeList)
                {
                    if (!modelResult.TypeName.Equals("datetimeV2.time")) continue;
                    IEnumerable<KeyValuePair<string, object>> result = modelResult.Resolution;
                    foreach (KeyValuePair<string, object> pair in result)
                    {
                        List<Dictionary<string, string>> nextResult = (List<Dictionary<string, string>>) pair.Value;
                        foreach (Dictionary<string, string> dict in nextResult)
                        {
                            DiscordMember reactor = (DiscordMember) e.User;
                            TimexProperty parsed = new TimexProperty(dict["timex"]);
                            DateTime time = new DateTime(2000, 1, 1, parsed.Hour ?? 1, parsed.Minute ?? 1, 1);
                            string opTimeZoneId = database.GetUsersTimeZone(e.Message.Author.Id)?.TimeZoneId;
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
                            TimeZoneInfo opTimeZone = SystemTimeZones[opTimeZoneId] ;
                            TimeZoneInfo reactorTimeZone = SystemTimeZones[reactorTimeZoneId];
                            DateTime reactorsTime = TimeZoneInfo.ConvertTime(time, opTimeZone, reactorTimeZone);
                            DiscordEmbed reactorTimeEmbed = new DiscordEmbedBuilder()
                                .WithTitle("You requested a timezone conversion")
                                .AddField("Poster's Time", $"\"{modelResult.Text}\"")
                                .AddField("Your time", $"{reactorsTime:t}");

                            await reactor.SendMessageAsync(embed: reactorTimeEmbed);
                        }
                    }
                }
            }
        }
    }
}