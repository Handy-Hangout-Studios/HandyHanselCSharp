using System;
using System.Collections.Generic;
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

namespace HandyHansel
{
    class Program
    {
        private static Timer eventTimer;
        private static DiscordClient discord;
        private static CommandsNextExtension commands;
        private static InteractivityExtension interactivity;

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

            //discord.MessageCreated += PingMessageCreated;

            StartTimer();
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static void StartTimer()
        {
            SetTimer();
            discord.DebugLogger.LogMessage(LogLevel.Info, "Timer", "Timer has been started", DateTime.Now);
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
            discord.DebugLogger.LogMessage(LogLevel.Info, "Timer", "Timer event fired", DateTime.Now);
            IDataAccessProvider database = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            List<ScheduledEvent> allScheduledEvents = database.GetAllPastScheduledEvents();
            foreach (ScheduledEvent se in allScheduledEvents)
            {
                database.DeleteScheduledEvent(se);
                DiscordChannel channel = await discord.GetChannelAsync(se.ChannelId);
                discord.DebugLogger.LogMessage(LogLevel.Info, "Timer", $"Timer has sent embed to {channel.Name}", DateTime.Now);
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
        //private static async Task PingMessageCreated(MessageCreateEventArgs e)
        //{
        //    if (e.Message.Content.ToLower().StartsWith("ping"))
        //        await e.Message.RespondAsync("pong!");
        //}
    }
}