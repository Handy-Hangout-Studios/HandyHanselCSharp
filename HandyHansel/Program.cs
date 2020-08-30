using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Quartz.Util;

namespace HandyHansel
{
    class Program
    {
        private static Timer eventTimer;
        private static DiscordClient discord;
        private static CommandsNextExtension commands;
        private static InteractivityExtension interactivity;

        public static readonly Dictionary<string, TimeZoneInfo> SystemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToDictionary(tz => tz.Id);
        
        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Config config = new Config();
            discord = new DiscordClient(config.ClientConfig);
            commands = discord.UseCommandsNext(config.CommandsConfig);
            interactivity = discord.UseInteractivity(config.InteractivityConfig);

            //commands.RegisterCommands<DNDCommands>(); // Is currently empty and so becomes NULL during the typeof used in the DSharpPlus.CommandsNext code
            commands.RegisterCommands<GeneralCommands>();
            //commands.RegisterCommands<MinecraftCommands>(); // Is currently empty and so becomes NULL during the typeof used in the DSharpPlus.CommandsNext code
            commands.RegisterCommands<TimeCommands>();
            commands.RegisterCommands<EventCommands>();

            discord.MessageCreated += CheckForDate;
            discord.MessageReactionAdded += SendAdjustedDate;

            StartTimer();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void StartTimer()
        {
            SetTimer();
            discord.Logger.Log(LogLevel.Information, "Timer", "Timer has been started", DateTime.Now);
        }
        private static void SetTimer()
        {
            eventTimer = new Timer(6000);
            eventTimer.Elapsed += OnTimedEvent;
            eventTimer.AutoReset = true;
            eventTimer.Enabled = true;
        }

        private static async void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            discord.Logger.Log(LogLevel.Information, "Timer", "Timer event fired", DateTime.Now);
            IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            List<ScheduledEvent> allScheduledEvents = database.GetAllPastScheduledEvents();
            foreach (ScheduledEvent se in allScheduledEvents)
            {
                database.DeleteScheduledEvent(se);
                DiscordChannel channel = await discord.GetChannelAsync(se.ChannelId);
                discord.Logger.Log(LogLevel.Information, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
                DiscordEmbed embed = new DiscordEmbedBuilder()
                {
                    Author = new DiscordEmbedBuilder.EmbedAuthor()
                    {
                        IconUrl = discord.CurrentUser.AvatarUrl,
                        Name = discord.CurrentUser.Username,
                    },
                    Description = se.Event.EventDesc,
                    Title = se.Event.EventName,
                }.Build();
                
                await discord.SendMessageAsync(channel, embed: embed);
            }
        }

        private static DiscordEmoji _clock;
        private static DiscordEmoji Clock()
        {
            return _clock ??= DiscordEmoji.FromName(discord, ":clock:");
        }
        private static async Task CheckForDate(MessageCreateEventArgs e)
        {
            List<ModelResult> dateTimeList =
                DateTimeRecognizer.RecognizeDateTime(e.Message.Content, Culture.English);
            
            if (dateTimeList.Count > 0 && (dateTimeList.Any(elem => elem.TypeName.EndsWith("time") || elem.TypeName.EndsWith("date"))))
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
                    if (modelResult.TypeName.EndsWith("time") || modelResult.TypeName.EndsWith("date"))
                    {    
                        IEnumerable<KeyValuePair<string, object>> result = modelResult.Resolution.Cast<KeyValuePair<string, object>>();
                        foreach (KeyValuePair<string, object> pair in result)
                        {
                            List<Dictionary<string, string>> nextResult = (List<Dictionary<string, string>>) pair.Value;
                            foreach (Dictionary<string, string> dict in nextResult)
                            {
                                DiscordMember reactor = (DiscordMember) e.User;
                                TimexProperty parsed = new Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexProperty(dict["timex"]);
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
                                    .WithTitle($"You requested a timezone conversion")
                                    .AddField("Poster's Time", $"\"{modelResult.Text}\"")
                                    .AddField("Your time", $"{reactorsTime.ToString("t")}");

                                await reactor.SendMessageAsync(embed: reactorTimeEmbed);
                            }
                        }
                    }
                }
            }
        }
    }
}