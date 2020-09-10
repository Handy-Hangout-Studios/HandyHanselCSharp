using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using HandyHansel.Models;

namespace HandyHansel.Commands
{
    [Group("event"), Description("The event functionality's submodule.\n\nWhen used alone Handy Hansel will randomly choose an event and announce it to `@everyone`!")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class EventCommands : BaseCommandModule
    {
        private static readonly Random Random = new Random();

        [GroupCommand]
        // ReSharper disable once UnusedMember.Global
        public async Task ExecuteGroupAsync(CommandContext context)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            List<GuildEvent> guildEvents = dataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id).ToList();
            GuildEvent selectedEvent = guildEvents[Random.Next(guildEvents.Count)];
            DiscordEmbedBuilder eventEmbedBuilder = new DiscordEmbedBuilder();
            eventEmbedBuilder
                .WithTitle(selectedEvent.EventName)
                .WithDescription(selectedEvent.EventDesc);
            await context.RespondAsync("@everyone");
            await context.RespondAsync(embed: eventEmbedBuilder.Build());
        }
        
        [Command("schedule"), RequireUserPermissions(Permissions.Administrator), Description("Schedule an event for the time passed in.")]
        // ReSharper disable once UnusedMember.Global
        public async Task ScheduleGuildEvent(
            CommandContext context, 
            [Description("The channel to announce the event in")] 
            DiscordChannel announcementChannel, 
            [Description("The date to schedule the event for")]
            [RemainingText] 
            string datetimeString
            )
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            DateTime datetime = Parser.DateTimeV2DateTimeParse(datetimeString);
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to schedule an event for {datetime:g} in your timezone?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));

            UserTimeZone userTimeZone = dataAccessProvider.GetUsersTimeZone(context.User.Id);
            if (userTimeZone == null)
            {
                await context.RespondAsync(
                    $"{context.User.Mention}, you don't have your time set up... I'm not much use without your timezone set up. Please set up your timezone by typing ^time before you waste my time anymore.");
                return;
            }
            
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            await context.RespondAsync("Ok, which event do you want to schedule?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, interactivity, dataAccessProvider), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm 
                            => int.TryParse(xm.Content, out _) 
                               && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut)
            {
                await context.RespondAsync(
                    "You didn't respond in time to select an event. I didn't schedule anything.");
            }

            GuildEvent selectedEvent =
                dataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id)
                    .ToList()[int.Parse(result.Result.Content) - 1];
            TimeZoneInfo schedulerTimeZoneInfo = Program.SystemTimeZones[userTimeZone.TimeZoneId];
            DateTime eventDateTime = TimeZoneInfo.ConvertTime(datetime, schedulerTimeZoneInfo, TimeZoneInfo.Local);
            ScheduledEvent newEvent = new ScheduledEvent
            {
                GuildEventId = selectedEvent.Id,
                ScheduledDate = eventDateTime,
                ChannelId = announcementChannel.Id,
            };

            dataAccessProvider.AddScheduledEvent(newEvent);

            await context.RespondAsync($"You have scheduled the following event for {datetime:g} in your time zone");
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Client.CurrentUser.Username, iconUrl: context.Client.CurrentUser.AvatarUrl)
                .WithDescription(selectedEvent.EventDesc)
                .WithTitle(selectedEvent.EventName)
                .Build();

            await context.RespondAsync(embed: embed);
        }

        [Command("add"), RequireUserPermissions(Permissions.Administrator), Description("Starts the set-up process for a new event to be added to the guild events for this server.")]
        // ReSharper disable once UnusedMember.Global
        public async Task AddGuildEvent(CommandContext context)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            DiscordMessage msg = await context.RespondAsync($":wave: Hi, {context.User.Mention}! You wanted to create a new event?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well, thanks for wasting my time. Have a good day.");
                return;
            }

            await context.RespondAsync("Ok, what do you want the event name to be?");
            InteractivityResult<DiscordMessage> result = await
                interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User));

            if (result.TimedOut) return;

            string eventName = result.Result.Content;
            
            await context.RespondAsync("What do you want the event description to be?");
            result = await
                interactivity.WaitForMessageAsync(xm => xm.Author.Equals(context.User));

            if (result.TimedOut) return;

            string eventDesc = result.Result.Content;

            GuildEvent newEvent = new GuildEvent
            {
                EventName = eventName,
                EventDesc = eventDesc,
                GuildId = context.Guild.Id,
            };

            dataAccessProvider.AddGuildEvent(newEvent);
            DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithAuthor(context.Client.CurrentUser.Username, iconUrl: context.Client.CurrentUser.AvatarUrl)
                .WithDescription(newEvent.EventDesc)
                .WithTitle(newEvent.EventName)
                .Build();

            await context.RespondAsync("You have added the following event to your guild:");
            await context.RespondAsync(embed: embed);
        }

        [Command("remove"), RequireUserPermissions(Permissions.Administrator), Description("Removes an event from the guild's events.")]
        // ReSharper disable once UnusedMember.Global
        public async Task RemoveGuildEvent(CommandContext context)
        {
            using IDataAccessProvider dataAccessProvider = new DataAccessPostgreSqlProvider(new PostgreSqlContext());
            DiscordMessage msg = await context.RespondAsync(
                $":wave: Hi, {context.User.Mention}! You want to remove an event from your guild list?");
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(context.Client, ":regional_indicator_n:"));
            InteractivityExtension interactivity = context.Client.GetInteractivity();

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, context.User);

            if (interactivityResult.TimedOut || 
                !interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(context.Client, ":regional_indicator_y:")))
            {
                await context.RespondAsync("Well then why did you get my attention! Thanks for wasting my time.");
                return;
            }

            await context.RespondAsync("Ok, which event do you want to remove?");

            _ = interactivity.SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, interactivity, dataAccessProvider), behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));

            await context.RespondAsync("Choose an event by typing: <event number>");

            InteractivityResult<DiscordMessage> result = await interactivity.WaitForMessageAsync(
                xm => int.TryParse(xm.Content, out _) && xm.Author.Equals(context.User),
                TimeSpan.FromMinutes(1));

            if (result.TimedOut) return;

            GuildEvent selectedEvent =
                dataAccessProvider.GetAllAssociatedGuildEvents(context.Guild.Id)
                    .ToList()[int.Parse(result.Result.Content) - 1];
            
            dataAccessProvider.DeleteGuildEvent(selectedEvent);
        }

        [Command("show"), RequireUserPermissions(Permissions.Administrator), Description("Shows a listing of all events currently available for this guild.")]
        // ReSharper disable once UnusedMember.Global
        public async Task ShowGuildEvents(CommandContext context)
        {
            await context.Client.GetInteractivity().SendPaginatedMessageAsync(context.Channel, context.User,
                GetGuildEventsPages(context.Guild.Id, context.Client.GetInteractivity(), new DataAccessPostgreSqlProvider(new PostgreSqlContext())),
                behaviour: PaginationBehaviour.WrapAround, timeoutoverride: TimeSpan.FromMinutes(1));
        }
        private static IEnumerable<Page> GetGuildEventsPages(ulong guildId, InteractivityExtension interactivity, IDataAccessProvider dataAccessProvider)
        {
            StringBuilder guildEventsStringBuilder = new StringBuilder();
            List<GuildEvent> guildEvents = dataAccessProvider.GetAllAssociatedGuildEvents(guildId).ToList();

            int count = 1;
            foreach (GuildEvent guildEvent in guildEvents)
            {
                guildEventsStringBuilder.AppendLine($"{count}. {guildEvent.EventName}");
                count++;
            }

            return interactivity.GeneratePagesInEmbed(guildEventsStringBuilder.ToString(), SplitType.Line);
        }    
    }
}